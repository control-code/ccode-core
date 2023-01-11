using Ccode.Domain;
using Ccode.AdaptersImpl.StateStore.InMemory;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Ccode.AdaptersImpl.Repository.UnitTests
{
	public class RepositoryTests
	{
		private readonly Guid _initiatorId = Guid.NewGuid();
		private readonly Guid _correlationId = Guid.NewGuid();
		private readonly Context _context;
		private readonly InMemoryStateStore _store = new InMemoryStateStore();
		private readonly Repository<TestAggregateRoot> _repository;

		private readonly TestAggregateRoot _root = new TestAggregateRoot(Guid.NewGuid(), 1, "Test");

		public RepositoryTests() 
		{
			_context = new Context(_initiatorId, _correlationId);
			_repository = new Repository<TestAggregateRoot>(_store, Array.Empty<Type>());
		}

		[Fact]
		public void AddAndGetEntity()
		{
			_repository.Add(_root, _context).Wait();
			var rootCopy = _repository.Get(_root.Id).Result;

			Assert.NotNull(rootCopy);
			Assert.Equal(_root.State, rootCopy.State);
		}

		[Fact]
		public void AddAndUpdateEntity()
		{
			var newText = "New text";

			_repository.Add(_root, _context).Wait();
			_root.SetText(newText);
			_repository.Update(_root, _context).Wait();
			var rootCopy = _repository.Get(_root.Id).Result;

			Assert.NotNull(rootCopy);
			Assert.Equal(_root.State, rootCopy.State);
			Assert.Equal(newText, rootCopy.State.Text);
		}

		[Fact]
		public void AddAndDeleteEntity()
		{
			_repository.Add(_root, _context).Wait();
			_repository.Delete(_root, _context).Wait();
			var rootCopy = _repository.Get(_root.Id).Result;

			Assert.Null(rootCopy);
		}
	}
}
