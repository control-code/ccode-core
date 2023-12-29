using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Ccode.Contracts.StateQueryAdapter;
using Ccode.Contracts.StateStoreAdapter;
using Ccode.Domain;
using Ccode.Contracts.StateEventAdapter;
using Microsoft.Extensions.Hosting;

namespace Ccode.Infrastructure.MongoStateStoreAdapter
{
	public record MongoStateStoreAdapterConfig
	{
		public string ConnectionString { get; init; } = string.Empty;
	}	

	public class MongoStateStoreAdapter : IStateStoreAdapter, IStateQueryAdapter, IStateEventAdapter, IHostedService, IDisposable
	{
		private class HistoryCollectionItem
		{
			private long _lastEventNumber;

			public HistoryCollectionItem(IMongoCollection<HistoryRecord> collection, long lastEventNumber)
			{
				Collection = collection;
				_lastEventNumber = lastEventNumber;
			}

			public IMongoCollection<HistoryRecord> Collection { get; }

			public long GetNextEventNumber()
			{
				return ++_lastEventNumber;
			}
		};

		private record HistoryKey(Guid Uid, long Version);
		private record HistoryRecord(long Id, HistoryKey Key, BsonDocument State, long EventNumber);

		private readonly MongoClient _client;
		private readonly IMongoDatabase _database;
		private readonly ConcurrentDictionary<string, IMongoCollection<BsonDocument>> _collections = new();
		private readonly ConcurrentDictionary<string, HistoryCollectionItem> _historyCollections = new();

		public MongoStateStoreAdapter(IOptions<MongoStateStoreAdapterConfig> options)
		{
			var connectionString = options.Value.ConnectionString;
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentException("ConnectionString cannot be empty", nameof(connectionString));

			_client = new MongoClient(connectionString);
			_database = _client.GetDatabase(MongoUrl.Create(connectionString).DatabaseName);

			var pack = new ConventionPack
			{
				new CamelCaseElementNameConvention(),
				new IgnoreExtraElementsConvention(true)
			};
			ConventionRegistry.Register("StateStore", pack, _ => true);
		}

		public async Task AddRoot<TRootState>(Guid uid, TRootState state, Context context) where TRootState : class
		{
			var collection = GetCollection(state.GetType());
			var historyCollectionItem = GetHistoryCollection(state.GetType());

			var document = state.ToBsonDocument();
			document.Add("_id", BsonValue.Create(uid));

			var historyRecord = new HistoryRecord(historyCollectionItem.GetNextEventNumber(), new HistoryKey(uid, 0), state.ToBsonDocument(), context.EventNumber);

			using (var session = await _client.StartSessionAsync())
			{
				session.StartTransaction();

				await collection.InsertOneAsync(session, document);
				await historyCollectionItem.Collection.InsertOneAsync(session, historyRecord);

				await session.CommitTransactionAsync();
			}
		}

		public Task DeleteRoot<TRootState>(Guid uid, Context context)
		{
			throw new NotImplementedException();
		}

		public async Task<TState?> Get<TState>(Guid uid) where TState : class
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", uid);

			var stateType = typeof(TState);
			var collection = GetCollection(stateType);
			var documents = await collection.FindAsync(filter);
			var doc = await documents.SingleOrDefaultAsync();

			if (doc == null) return null;

			return (TState)BsonSerializer.Deserialize(doc, stateType);
		}

		public async Task<long> GetMaxEventNumber<TState>() where TState : class
		{
			var type = typeof(TState);
			var collection = _database.GetCollection<HistoryRecord>(GetHistoryCollectionName(type));
			var pipeline = new[]
			{
				new BsonDocument("$sort", new BsonDocument("EventNumber", -1)),
				new BsonDocument("$limit", 1)
			};

			var result = await collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
			var maxValue = result != null ? result["eventNumber"].AsInt64 : -1L;

			return maxValue;
		}

		private IMongoCollection<BsonDocument> GetCollection(Type type)
		{
			return _collections.GetValueOrDefault(type.Name,
				_database.GetCollection<BsonDocument>(GetCollectionName(type)));
		}

		private HistoryCollectionItem GetHistoryCollection(Type type)
		{
			var name = type.Name + "History";
			return _historyCollections.GetValueOrDefault(name, GetHistoryCollectionItem(type));
		}

		private HistoryCollectionItem GetHistoryCollectionItem(Type type)
		{
			var collection = _database.GetCollection<HistoryRecord>(GetHistoryCollectionName(type));
			var pipeline = new[]
			{
				new BsonDocument("$sort", new BsonDocument("_id", -1)),
				new BsonDocument("$limit", 1)
			};

			var result = collection.Aggregate<BsonDocument>(pipeline).FirstOrDefault();
			var maxValue = result != null ? result["_id"].AsInt64 : -1L;

			return new HistoryCollectionItem(collection, maxValue);
		}

		private static string GetCollectionName(Type type)
		{
			var name = type.Name;
			return char.ToLower(name[0]) + name[1..] + "s";
		}

		private static string GetHistoryCollectionName(Type type)
		{
			var name = type.Name;
			return char.ToLower(name[0]) + name[1..] + "sHistory";
		}

		public async Task<IEnumerable<Guid>> GetUids<TState>(string fieldName, string fieldValue) where TState : class
		{
			var filter = Builders<BsonDocument>.Filter.Eq(fieldName, fieldValue);
			var fields = Builders<BsonDocument>.Projection.Include("_id");

			var stateType = typeof(TState);
			var collection = GetCollection(stateType);
			var documents = await collection.Find(filter).Project(fields).ToListAsync();

			return documents.Select(d => d["_id"].AsGuid);
		}

		public async Task<IEnumerable<EntityState<TState>>> Get<TState>(string fieldName, string fieldValue) where TState : class
		{
			var filter = Builders<BsonDocument>.Filter.Eq(fieldName, fieldValue);

			var stateType = typeof(TState);
			var collection = GetCollection(stateType);
			var documents = await collection.Find(filter).ToListAsync();

			return documents.Select(d => new EntityState<TState>(d["_id"].AsGuid, (TState)BsonSerializer.Deserialize(d, stateType)));
		}

		public async Task<IEnumerable<EntityState<TState>>> GetAll<TState>() where TState : class
		{
			var stateType = typeof(TState);
			var collection = GetCollection(stateType);
			var documents = await collection.Find(_ => true).ToListAsync();

			return documents.Select(d => new EntityState<TState>(d["_id"].AsGuid, (TState)BsonSerializer.Deserialize(d, stateType)));
		}

		private class EventPoller
		{
			private Type _stateType;
			private IMongoCollection<HistoryRecord> _collection;
			private List<Func<StateStoreEvent, Task>> _callbacks = new();
			private long _lastProcessedEventNumber = -1;
			private SemaphoreSlim _semaphore = new (1, 1);

			public EventPoller(IMongoCollection<HistoryRecord> collection, Type stateType)
			{
				_collection = collection;
				_stateType = stateType;
			}

			public async Task PollEvents()
			{
				await _semaphore.WaitAsync();
				try
				{
					var records = await GetHistoryRecordsAsync(_lastProcessedEventNumber);
					foreach (var record in records)
					{
						var e = new StateStoreEvent(record.Id, StateStoreEventType.RootAdded, _stateType, record.Key.Uid, BsonSerializer.Deserialize(record.State, _stateType));
						var tasks = _callbacks.Select(c => c.Invoke(e));
						Task.WaitAll(tasks.ToArray());
						_lastProcessedEventNumber = record.Id;
					}
				}
				finally { _semaphore.Release(); }
			}

			public async Task Subscribe(long lastProcessedEventNumber, Func<StateStoreEvent, Task> callback)
			{
				await _semaphore.WaitAsync();
				try
				{
					var records = await GetHistoryRecordsAsync(lastProcessedEventNumber, _lastProcessedEventNumber);
					foreach (var record in records)
					{
						var e = new StateStoreEvent(record.Id, StateStoreEventType.RootAdded, _stateType, record.Key.Uid, BsonSerializer.Deserialize(record.State, _stateType));
						await callback.Invoke(e);
					}
					_callbacks.Add(callback);
				}
				finally { _semaphore.Release(); }
			}

			public async Task Unsubscribe(Func<StateStoreEvent, Task> callback)
			{
				await _semaphore.WaitAsync();
				try
				{
					_callbacks.Remove(callback);
				}
				finally { _semaphore.Release(); }
			}

			private async Task<List<HistoryRecord>> GetHistoryRecordsAsync(long lastProcessedEventNumber, long toEventNumber = long.MaxValue)
			{
				var filter = toEventNumber < long.MaxValue ?
					Builders<HistoryRecord>.Filter.Where(h => h.Id > lastProcessedEventNumber && h.Id <= toEventNumber):
					Builders<HistoryRecord>.Filter.Where(h => h.Id > lastProcessedEventNumber);

				return await _collection
					.Find(filter)
					.SortBy(h => h.Id)
					.ToListAsync();
			}
		}

		private ConcurrentDictionary<Guid, EventPoller> _poolers = new();

		public async Task PollEvents()
		{
			foreach(var pooler in _poolers.Values)
			{
				await pooler.PollEvents();
			}
		}

		public Task Subscribe<TRootState>(long lastProcessedEventNumber, Func<StateStoreEvent, Task> callback)
		{
			var type = typeof(TRootState);
			var p = _poolers.GetOrAdd(type.GUID, h =>
				{
					var collection = _database.GetCollection<HistoryRecord>(GetHistoryCollectionName(type));
					return new EventPoller(collection, type);
				});

			return p.Subscribe(lastProcessedEventNumber, callback);
		}

		public async Task Unsubscribe<TRootState>(Func<StateStoreEvent, Task> callback)
		{
			var type = typeof(TRootState);
			if(_poolers.TryGetValue(type.GUID, out var poller))
			{
				await poller.Unsubscribe(callback);
			}
		}

		private Timer? _timer = null;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_timer = new Timer(ProcessEvents, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		private void ProcessEvents(object? state)
		{
			PollEvents().Wait();
		}
	}
}
