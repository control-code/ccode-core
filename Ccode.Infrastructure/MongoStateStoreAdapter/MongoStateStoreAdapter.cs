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

namespace Ccode.Infrastructure.MongoStateStoreAdapter
{
	public record MongoStateStoreAdapterConfig
	{
		public string ConnectionString { get; init; } = string.Empty;
	}	

	public class MongoStateStoreAdapter : IStateStoreAdapter, IStateQueryAdapter, IStateEventAdapter
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
		private record HistoryRecord(HistoryKey Key, BsonDocument State, long EventNumber);

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

			var historyRecord = new HistoryRecord(new HistoryKey(uid, 0), state.ToBsonDocument(), historyCollectionItem.GetNextEventNumber());

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
				new BsonDocument("$sort", new BsonDocument("eventNumber", -1)),
				new BsonDocument("$limit", 1)
			};

			var result = collection.Aggregate<BsonDocument>(pipeline).FirstOrDefault();
			var maxValue = result != null ? result["eventNumber"].AsInt64 : -1L;

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

		public class Subscription
		{
			public Guid EntityUid { get; private set; }

			private Func<StateStoreEvent, Task> _callbacks;

			public Subscription(Guid entityUid, Func<StateStoreEvent, Task> callback, long lastProcessedEventNumber)
			{
				EntityUid = entityUid;
				_callbacks = callback;
			}

			public void AddCallback(Func<StateStoreEvent, Task> callback, long lastProcessedEventNumber)
			{
				_callbacks += callback;
			}
		}

		private class EventPoller<TState>
		{
			private IMongoCollection<HistoryRecord> _collection;
			private ConcurrentDictionary<Guid, Subscription> _subscriptions = new ConcurrentDictionary<Guid, Subscription>();

			public EventPoller(IMongoCollection<HistoryRecord> collection)
			{
				_collection = collection;
			}
			/*
			public Task PollEvents()
			{
				var records = GetHistoryRecordsAsync()

			}

			public Task Subscribe(long lastProcessedEventNumber, Func<StateStoreEvent, Task> callback)
			{
				_subscriptions.GetOrAdd()
			}

			private async Task<List<HistoryRecord>> GetHistoryRecordsAsync(
				Guid uid,
				int lastProcessedVersionNumber)
			{
				var filter = Builders<HistoryRecord>.Filter
					.Where(h =>
						h.Key.Uid == uid &&
						h.Key.Version > lastProcessedVersionNumber);

				return await _collection
					.Find(filter)
					.SortByDescending(h => h.Key.Version)
					.ToListAsync();
			}
			*/
		}



		public Task Subscribe<TRootState>(long lastProcessedEventNumber, Func<StateStoreEvent, Task> callback)
		{
			throw new NotImplementedException();
		}

		public Task Unsubscribe<TRootState>(Func<StateStoreEvent, Task> callback)
		{
			throw new NotImplementedException();
		}
	}
}
