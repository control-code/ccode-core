using Ccode.AdaptersImpl.UnitTests;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.StateStore.Mongo.Tests
{
	/*
	"rootType": "TestRootEntityState",
	"subentityTypes": [
		"Ccode.AdaptersImpl.UnitTests.TestSubentityState, Ccode.AdaptersImpl.UnitTests"
	]
	*/
	
	internal static class Program
	{
		private static readonly Guid _rootId = Guid.NewGuid();
		private static readonly Guid _initiatorId = Guid.NewGuid();
		private static readonly Guid _operationId = Guid.NewGuid();
		private static readonly Context _context = new(_initiatorId, _operationId);

		private static readonly MongoStateStore _store = new("mongodb://localhost:27017/stateStoreTest"); 
			
		static void Main()
		{
			RunTest(MongoStateStoreAddTest);
			RunTest(MongoStateStoreUpdateTest);
			RunTest(MongoStateStoreGetTest);
			RunTest(MongoStateStoreGetByRootTest);
			RunTest(MongoStateStoreDeleteTest);
		}

		static void RunTest(Action test)
		{
			Console.Write($"Run {test.Method.Name} - ");
			test.Invoke();
			Console.WriteLine("Ok");
		}
		
		static void MongoStateStoreAddTest()
		{
			var state = new TestRootEntityState(1, "Test");

			_store.AddRoot(_rootId, state, _context).Wait();
		}

		static void MongoStateStoreUpdateTest()
		{
			var state = new TestRootEntityState(2, "Test new");

			_store.Update(_rootId, state, _context).Wait();
		}

		static void MongoStateStoreDeleteTest()
		{
			_store.Delete<TestRootEntityState>(_rootId, _context).Wait();
		}

		static void MongoStateStoreGetTest()
		{
			var state = _store.Get<TestRootEntityState>(_rootId).Result;

			if (state == null)
			{
				throw new Exception();
			}
		}

		static void MongoStateStoreGetByRootTest()
		{
			var states = _store.GetByRoot<TestRootEntityState>(_rootId).Result;

			if (states == null || states.Substates is not { Length: 0 })
			{
				throw new Exception();
			}
		}

	}
}