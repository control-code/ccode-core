using Ccode.Domain;
using Ccode.AdaptersImpl.StateStore.InMemory;

namespace Ccode.AdaptersImpl.UnitTests
{
	public class InMemoryStateStoreTests
	{
		private readonly InMemoryStateStore _store = new InMemoryStateStore();
		private readonly Context _context = new Context(Guid.NewGuid(), Guid.NewGuid());

		private readonly Guid _rootId = Guid.NewGuid();
		private readonly TestRootEntityState _rootState = new TestRootEntityState(1, "Test");

		private readonly Tuple<Guid, TestSubentityState>[] _substates = new Tuple<Guid, TestSubentityState>[]
			{
				new Tuple<Guid, TestSubentityState>(Guid.NewGuid(), new TestSubentityState(0.01)),
				new Tuple<Guid, TestSubentityState>(Guid.NewGuid(), new TestSubentityState(0.02)),
				new Tuple<Guid, TestSubentityState>(Guid.NewGuid(), new TestSubentityState(0.03))
			};

		[Fact]
		public void AddAndGetState()
		{
			_store.AddRoot(_rootId, _rootState, _context).Wait();

			var state2 = _store.Get<TestRootEntityState>(_rootId).Result;

			Assert.Equal(_rootState, state2);
		}

		[Fact]
		public void AddUpdateAndGetState()
		{
			_store.AddRoot(_rootId, _rootState, _context).Wait();

			var stateNew = _rootState with { Number = 2 };
			_store.Update(_rootId, stateNew, _context).Wait();

			var state2 = _store.Get<TestRootEntityState>(_rootId).Result;

			Assert.NotEqual(_rootState, state2);
			Assert.Equal(stateNew, state2);
		}

		[Fact]
		public void AddAndDeleteState()
		{
			_store.AddRoot(_rootId, _rootState, _context).Wait();
			_store.DeleteRoot<TestRootEntityState>(_rootId, _context).Wait();

			var state2 = _store.Get<TestRootEntityState>(_rootId).Result;

			Assert.Null(state2);
		}

		[Fact]
		public void AddAndGetSubstates()
		{
			_store.AddRoot(_rootId, _rootState, _context).Wait();
			
			foreach(var substateItem in _substates)
			{
				_store.Add(substateItem.Item1, _rootId, substateItem.Item2, _context).Wait();
			}

			foreach (var substateItem in _substates)
			{
				var substate = _store.Get<TestSubentityState>(substateItem.Item1).Result;
				Assert.Equal(substateItem.Item2, substate);
			}
		}

		[Fact]
		public void AddAndDeleteWithSubstates()
		{
			_store.AddRoot(_rootId, _rootState, _context).Wait();

			foreach (var substateItem in _substates)
			{
				_store.Add(substateItem.Item1, _rootId, substateItem.Item2, _context).Wait();
			}

			_store.DeleteRoot<TestRootEntityState>(_rootId, _context).Wait();

			var state2 = _store.Get<TestRootEntityState>(_rootId).Result;
			Assert.Null(state2);

			foreach (var substateItem in _substates)
			{
				var substate = _store.Get<TestSubentityState>(substateItem.Item1).Result;
				Assert.Null(substate);
			}
		}
	}
}
