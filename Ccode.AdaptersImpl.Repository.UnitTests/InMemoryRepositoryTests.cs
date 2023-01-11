using Ccode.Domain;
using Ccode.AdaptersImpl.Repository.InMemory;
using Ccode.AdaptersImpl.UnitTests;

namespace Ccode.AdaptersImpl.Repository.UnitTests
{
	public class InMemoryRepositoryTests
	{
		private readonly Guid _initiatorId = Guid.NewGuid();
		private readonly Guid _correlationId = Guid.NewGuid();
		private readonly Context _context;
		private readonly InMemoryRepository<TestAggregateRoot, TestRootEntityState> _repository = 
			new InMemoryRepository<TestAggregateRoot, TestRootEntityState>();

		private readonly TestAggregateRoot _root = new TestAggregateRoot(Guid.NewGuid(), 1, "Test");

		public InMemoryRepositoryTests()
		{
			_context = new Context(_initiatorId, _correlationId);
		}

		[Fact]
		public void AddEntity()
		{
			_repository.Add(_root, _context).Wait();

			Assert.Equal(1, _repository.StateEvents.Count);
			Assert.Equal(StateEventOperation.Add, _repository.StateEvents.First().Operation);
			Assert.Equal(1, _repository.EntityStates.Count);
		}

		[Fact]
		public void UpdateEntity()
		{
			_repository.Add(_root, _context).Wait();
			_root.SetText("New data");
			_repository.Update(_root, _context).Wait();

			Assert.Equal(2, _repository.StateEvents.Count);
			Assert.Equal(StateEventOperation.Add, _repository.StateEvents.First().Operation);
			Assert.Equal(StateEventOperation.Update, _repository.StateEvents.Skip(1).First().Operation);
			Assert.Equal(1, _repository.EntityStates.Count);
		}

		[Fact]
		public void DeleteEntity()
		{
			_repository.Add(_root, _context).Wait();
			_repository.Delete(_root, _context).Wait();

			Assert.Equal(2, _repository.StateEvents.Count);
			Assert.Equal(StateEventOperation.Add, _repository.StateEvents.First().Operation);
			Assert.Equal(StateEventOperation.Delete, _repository.StateEvents.Skip(1).First().Operation);
			Assert.Equal(0, _repository.EntityStates.Count);
		}

		[Fact]
		public void GetEntity()
		{
			_repository.Add(_root, _context);
			_root.SetText("New data");
			_repository.Update(_root, _context);

			var entityB = _repository.Get(_root.Id).Result;

			Assert.True(_root == entityB); // same object
		}
	}
}