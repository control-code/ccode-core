using Ccode.Adapters.StateStore;
using Ccode.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Ccode.AdaptersImpl.StateStore.Mongo
{
	public class MongoStateStore : IStateStore
	{
		/*
			"rootType": "TestRootEntityState",
			"subentityTypes": [
				"Ccode.AdaptersImpl.UnitTests.TestSubentityState, Ccode.AdaptersImpl.UnitTests"
			]
		 */


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
			ConventionRegistry.Register("StateStore", pack, t => true);

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
			return Add(id, id, null, state, context);
		}

		public Task Add(Guid id, Guid rootId, object state, Context context)
		{
			return Add(id, rootId, null, state, context);
		}

		public Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context)
		{
			var collection = GetCollection(state.GetType());
			var document = state.ToBsonDocument();
			document.Add("_id", BsonValue.Create(id));
			document.Add("rootId", BsonValue.Create(rootId));
			if (parentId.HasValue)
			{
				document.Add("parentId", BsonValue.Create(parentId.Value));
			}
			return collection.InsertOneAsync(document);
		}

		public Task Update(Guid id, object state, Context context)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

			var collection = GetCollection(state.GetType());
			var document = state.ToBsonDocument();
			var updateDocument = new BsonDocument("$set", document);
			return collection.UpdateOneAsync(filter, updateDocument);
		}

		public Task Delete<TState>(Guid id, Context context)
		{
			return Delete(typeof(TState), id, context);
		}

		public Task Delete(Type stateType, Guid id, Context context)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

			var collection = GetCollection(stateType);
			return collection.DeleteOneAsync(filter);
		}

		public Task DeleteByRoot<TState>(Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public Task DeleteByRoot(Type stateType, Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public Task DeleteWithSubstates<TState>(Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public Task DeleteWithSubstates(Type stateType, Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public async Task<object?> Get<TState>(Guid id)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", id);

			var collection = GetCollection(typeof(TState));
			var documents = await collection.FindAsync(filter);
			var doc = await documents.SingleOrDefaultAsync();

			if (doc == null) return null;

			return BsonSerializer.Deserialize(doc, typeof(TState));
		}

		public Task<object?> Get(Type stateType, Guid id)
		{
			throw new NotImplementedException();
		}

		public Task<EntityData[]> GetByRoot<TState>(Guid rootId)
		{
			throw new NotImplementedException();
		}

		public Task<EntityData[]> GetByRoot(Type stateType, Guid rootId)
		{
			throw new NotImplementedException();
		}

		private IMongoCollection<BsonDocument> GetCollection(Type type)
		{
			return _collections.GetValueOrDefault(type.Name, _database.GetCollection<BsonDocument>(GetCollectionName(type)));
		}

		public Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			throw new NotImplementedException();
		}

		private string GetCollectionName(Type type) 
		{
			var name = type.Name;
			return char.ToLower(name[0]) + name.Substring(1) + "s";
		}
	}
}