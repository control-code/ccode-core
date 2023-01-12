using Ccode.AdaptersImpl.UnitTests;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.StateStore.Mongo.Tests
{
	internal class Program
	{
		private static readonly Guid _rootId = Guid.NewGuid();
		private static readonly Guid _initiatorId = Guid.NewGuid();
		private static readonly Guid _operationId = Guid.NewGuid();
		private static readonly Context _context = new Context(_initiatorId, _operationId);

		static void Main(string[] args)
		{
			var store = new MongoStateStore("mongodb://localhost:27017/stateStoreTest");
			MongoStateStoreAddTest(store);
			MongoStateStoreUpdateTest(store);
			MongoStateStoreGetTest(store);
			MongoStateStoreDeleteTest(store);
		}

		static void MongoStateStoreAddTest(MongoStateStore store)
		{
			var state = new TestRootEntityState(1, "Test");

			store.Add(_rootId, state, _context).Wait();
		}

		static void MongoStateStoreUpdateTest(MongoStateStore store)
		{
			var state = new TestRootEntityState(2, "Test new");

			store.Update(_rootId, state, _context).Wait();
		}

		static void MongoStateStoreDeleteTest(MongoStateStore store)
		{
			store.Delete<TestRootEntityState>(_rootId, _context).Wait();
		}

		static void MongoStateStoreGetTest(MongoStateStore store)
		{
			Guid id = _rootId;

			var state = store.Get<TestRootEntityState>(id).Result;

			if (state == null)
			{
				throw new Exception();
			}
		}
	}
}