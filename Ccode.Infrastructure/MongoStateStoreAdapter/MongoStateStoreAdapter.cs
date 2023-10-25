using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Ccode.Contracts.StateQueryAdapter;
using Ccode.Contracts.StateStoreAdapter;
using Ccode.Domain;

namespace Ccode.Infrastructure.MongoStateStoreAdapter
{
	public record MongoStateStoreAdapterConfig
	{
		public string ConnectionString { get; init; } = string.Empty;
	}

	public class MongoStateStoreAdapter : IStateStoreAdapter, IStateQueryAdapter
	{
		private readonly MongoClient _client;
		private readonly IMongoDatabase _database;
		private readonly ConcurrentDictionary<string, IMongoCollection<BsonDocument>> _collections = new();

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

		public Task AddRoot<TRootState>(Guid uid, TRootState state, Context context) where TRootState : class
		{
			var collection = GetCollection(state.GetType());
			var document = state.ToBsonDocument();
			document.Add("_id", BsonValue.Create(uid));

			return collection.InsertOneAsync(document);
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

		private static string GetCollectionName(Type type)
		{
			var name = type.Name;
			return char.ToLower(name[0]) + name[1..] + "s";
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
	}
}
