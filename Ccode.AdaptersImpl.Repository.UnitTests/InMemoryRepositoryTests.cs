using Ccode.AdaptersImpl.Repository.InMemory;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.Repository.UnitTests
{
	public class InMemoryRepositoryTests
	{
		private record RootState(string Data);

		private class Root : AggregateRoot<RootState>
		{
			public Root(Guid id) : base(id, new RootState(string.Empty))
			{
			}

			public void SetData(string newData)
			{
				State = State with { Data = newData };
			}
		}

		private readonly Guid _initiatorId = Guid.NewGuid();
		private readonly Guid _correlationId = Guid.NewGuid();
		private readonly Context _context;

		public InMemoryRepositoryTests()
		{
			_context = new Context(_initiatorId, _correlationId);
		}

		[Fact]
		public void AddEntity()
		{
			var r = new InMemoryRepository<Root, RootState>();

			var id = Guid.NewGuid();
			var entity = new Root(id);

			r.Add(entity, _context).Wait();

			Assert.Equal(1, r.StateEvents.Count);
			Assert.Equal(StateEventOperation.Add, r.StateEvents.First().Operation);
			Assert.Equal(1, r.EntityStates.Count);
		}

		[Fact]
		public void UpdateEntity()
		{
			var r = new InMemoryRepository<Root, RootState>();

			var id = Guid.NewGuid();
			var entity = new Root(id);

			r.Add(entity, _context).Wait();
			entity.SetData("New data");
			r.Update(entity, _context).Wait();

			Assert.Equal(2, r.StateEvents.Count);
			Assert.Equal(StateEventOperation.Add, r.StateEvents.First().Operation);
			Assert.Equal(StateEventOperation.Update, r.StateEvents.Skip(1).First().Operation);
			Assert.Equal(1, r.EntityStates.Count);
		}

		[Fact]
		public void DeleteEntity()
		{
			var r = new InMemoryRepository<Root, RootState>();

			var id = Guid.NewGuid();
			var entity = new Root(id);

			r.Add(entity, _context).Wait();
			r.Delete(entity, _context).Wait();

			Assert.Equal(2, r.StateEvents.Count);
			Assert.Equal(StateEventOperation.Add, r.StateEvents.First().Operation);
			Assert.Equal(StateEventOperation.Delete, r.StateEvents.Skip(1).First().Operation);
			Assert.Equal(0, r.EntityStates.Count);
		}

		[Fact]
		public void GetEntity()
		{
			var r = new InMemoryRepository<Root, RootState>();

			var id = Guid.NewGuid();
			var entityA = new Root(id);

			r.Add(entityA, _context);
			entityA.SetData("New data");
			r.Update(entityA, _context);

			var entityB = r.Get(id).Result;

			Assert.True(entityA == entityB);
		}
	}
}