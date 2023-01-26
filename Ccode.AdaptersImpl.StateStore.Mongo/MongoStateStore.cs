using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Ccode.Domain;
using Ccode.Adapters.StateStore;
using Ccode.Domain.Entities;

namespace Ccode.AdaptersImpl.StateStore.Mongo
{
	public class MongoStateStore : IStateStore
	{
		private class RootSubentityTypes
		{
			public string RootType { get; }

			public string[] SubentityTypes { get; }

			public RootSubentityTypes(string rootType, string[] subentityTypes)
			{
				RootType = rootType;
				SubentityTypes = subentityTypes;
			}
		}

		private readonly MongoClient _client;
		private readonly IMongoDatabase _database;
		private readonly SortedList<string, Type[]> _subentityTypes = new();
		private readonly ConcurrentDictionary<string, IMongoCollection<BsonDocument>> _collections = new();

		public MongoStateStore(string connectionString)
		{
			_client = new MongoClient(connectionString);
			_database = _client.GetDatabase(MongoUrl.Create(connectionString).DatabaseName);

			var pack = new ConventionPack
			{
				new CamelCaseElementNameConvention(),
				new IgnoreExtraElementsConvention(true)
			};
			ConventionRegistry.Register("StateStore", pack, _ => true);

			var collection = _database.GetCollection<RootSubentityTypes>("rootSubentityTypes");
			var docs = collection.Find(_ => true).ToEnumerable();
			foreach(var d in docs)
			{
				_subentityTypes.Add(d.RootType, d.SubentityTypes.Select(name =>
				{
					var t = Type.GetType(name);
					if (t == null)
					{
						throw new TypeNotFoundException(name);
					}
					return t;
				}).ToArray());
			}
		}

		public Task Add(Guid id, object state, Context context)
		{
			return Add(null, id, id, null, state, context);
		}

		public Task Add(Guid id, Guid rootId, object state, Context context)
		{
			return Add(null, id, rootId, null, state, context);
		}

		public Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context)
		{
			return Add(null, id, rootId, null, state, context);
		}

		private Task Add(IClientSessionHandle? session, Guid id, Guid rootId, Guid? parentId, object state,
			Context context)
		{
			var collection = GetCollection(state.GetType());
			var document = state.ToBsonDocument();
			document.Add("_id", BsonValue.Create(id));
			document.Add("rootId", BsonValue.Create(rootId));
			if (parentId.HasValue)
			{
				document.Add("parentId", BsonValue.Create(parentId.Value));
			}

			return session != null ? collection.InsertOneAsync(session, document) : collection.InsertOneAsync(document);
		}

		public Task Update(Guid id, object state, Context context)
		{
			return Update(null, id, state, context);
		}

		private Task Update(IClientSessionHandle? session, Guid id, object state, Context context)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

			var collection = GetCollection(state.GetType());
			var document = state.ToBsonDocument();
			var updateDocument = new BsonDocument("$set", document);
			return session != null
				? collection.UpdateOneAsync(session, filter, updateDocument)
				: collection.UpdateOneAsync(filter, updateDocument);
		}

		public Task Delete<TState>(Guid id, Context context)
		{
			return Delete(null, typeof(TState), id, context);
		}

		public Task Delete(Type stateType, Guid id, Context context)
		{
			return Delete(null, stateType, id, context);
		}

		private Task Delete(IClientSessionHandle? session, Type stateType, Guid id, Context context)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

			var collection = GetCollection(stateType);
			return session != null ? collection.DeleteOneAsync(session, filter) : collection.DeleteOneAsync(filter);
		}
		
		public Task DeleteByRoot<TState>(Guid rootId, Context context)
		{
			return DeleteByRoot(typeof(TState), rootId, context);
		}

		public Task DeleteByRoot(Type stateType, Guid rootId, Context context)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("rootId", rootId);

			var collection = GetCollection(stateType);
			return collection.DeleteManyAsync(filter);
		}

		public Task DeleteWithSubstates<TState>(Guid rootId, Context context)
		{
			return DeleteWithSubstates(typeof(TState), rootId, context);
		}

		public async Task DeleteWithSubstates(Type stateType, Guid rootId, Context context)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("rootId", rootId);
			var types = _subentityTypes[stateType.Name];
			
			using var session = await _client.StartSessionAsync();
			session.StartTransaction();

			foreach (var t in types)
			{
				var collection = GetCollection(t);
				await collection.DeleteManyAsync(session, filter);
			}

			var rootCollection = GetCollection(stateType);
			await rootCollection.DeleteManyAsync(session, filter);
			
			await session.CommitTransactionAsync();
		}

		public Task<object?> Get<TState>(Guid id)
		{
			return Get(typeof(TState), id);
		}

		public async Task<object?> Get(Type stateType, Guid id)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

			var collection = GetCollection(stateType);
			var documents = await collection.FindAsync(filter);
			var doc = await documents.SingleOrDefaultAsync();

			if (doc == null) return null;

			return BsonSerializer.Deserialize(doc, stateType);
		}

		public Task<States?> GetByRoot<TState>(Guid rootId)
		{
			return GetByRoot(typeof(TState), rootId);
		}

		public async Task<States?> GetByRoot(Type stateType, Guid rootId)
		{
			var rootState = Get(stateType, rootId);

			if (rootState == null)
			{
				return null;
			}

			var filter = Builders<BsonDocument>.Filter.Eq("rootId", rootId);
			var substates = new List<StateInfo>();

			var types = _subentityTypes[stateType.Name];

			foreach (var t in types)
			{
				var collection = GetCollection(t);
				var documents = await collection.FindAsync(filter);

				while (await documents.MoveNextAsync())
				{
					substates.AddRange(documents.Current.Select(d =>
						new StateInfo(d["_id"].AsGuid, d["rootId"].AsGuid,
							d.TryGetValue("parentId", out var value) ? value.AsGuid : null,
							BsonSerializer.Deserialize(d, t))));
				}
			}

			return new States(rootState, substates.ToArray());
		}

		public async Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			using var session = await _client.StartSessionAsync();
			session.StartTransaction(); // handle all state changes in one transaction 

			foreach (var ev in events)
			{
				switch (ev.Operation)
				{
					case StateEventOperation.Add:
						await Add(session, ev.EntityId, rootId, ev.ParentId, ev.State, context);
						break;
					case StateEventOperation.Update:
						await Update(session, ev.EntityId, ev.State, context);
						break;
					case StateEventOperation.Delete:
						await Delete(session, ev.State.GetType(), ev.EntityId, context);
						break;
				}
			}

			await session.CommitTransactionAsync();
		}

		private IMongoCollection<BsonDocument> GetCollection(Type type)
		{
			return _collections.GetValueOrDefault(type.Name,
				_database.GetCollection<BsonDocument>(GetCollectionName(type)));
		}
		
		private string GetCollectionName(Type type) 
		{
			var name = type.Name;
			return char.ToLower(name[0]) + name.Substring(1) + "s";
		}
	}
}