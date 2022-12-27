using Ccode.Domain;
using Ccode.Adapters.StateStore;
using Ccode.Adapters.Repository;
using Ccode.AdaptersImpl.StateStore.MsSql;

namespace Ccode.AdaptersImpl.Repository.Tests
{
	internal class Program
	{
		private static readonly Guid _rootId = Guid.NewGuid();
		private static readonly Guid _initiatorId = Guid.NewGuid();
		private static readonly Guid _operationId = Guid.NewGuid();
		private static readonly Context _context = new Context(_initiatorId, _operationId);

		static void Main(string[] args)
		{
			RunTest(MsSqlRepositoryAddTest);
			RunTest(MsSqlRepositoryGetTest);
			RunTest(MsSqlRepositoryUpdateTest);
			RunTest(MsSqlRepositoryDeleteTest);
		}

		static void RunTest(Action test)
		{
			Console.Write($"Run {test.Method.Name} - ");
			test.Invoke();
			Console.WriteLine("Ok");
		}

		static IRepository<TestAggregateRoot> CreateRepository()
		{
			var stateStore = new MsSqlStateStore("Server=localhost\\SQLEXPRESS;Initial Catalog=StateStoreTest;Integrated Security = true");
			stateStore.StartAsync(CancellationToken.None).Wait();
			return new Repository<TestAggregateRoot>(stateStore, new[] { typeof(TestSubentityState) });
		}

		static void MsSqlRepositoryAddTest()
		{
			var repo = CreateRepository();

			Guid id = _rootId;

			var r = new TestAggregateRoot(id, new TestAggregateRootState(Random.Shared.Next(100)), new EntityData[0]);

			repo.Add(r, _context).Wait();
		}

		static void MsSqlRepositoryGetTest()
		{
			var repo = CreateRepository();

			Guid id = _rootId;

			var r = repo.Get(id).Result;

			if (r == null)
			{
				throw new Exception();
			}
		}

		static void MsSqlRepositoryUpdateTest()
		{
			var repo = CreateRepository();

			Guid id = _rootId;

			var r = repo.Get(id).Result;
			if (r == null)
			{
				throw new Exception();
			}

			r.SetNumber(Random.Shared.Next(100));
			r.AddTextItem("Test item text");

			repo.Update(r, _context).Wait();
		}

		static void MsSqlRepositoryDeleteTest()
		{
			var repo = CreateRepository();

			Guid id = _rootId;

			var r = repo.Get(id).Result;
			if (r == null)
			{
				throw new Exception();
			}

			repo.Delete(r, _context).Wait();
		}
	}
}