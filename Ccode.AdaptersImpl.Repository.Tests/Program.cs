using Ccode.Domain;
using Ccode.AdaptersImpl.Repository.MsSql;

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
			MsSqlRepositoryAddTest();
			MsSqlRepositoryGetTest();
			MsSqlRepositoryUpdateTest();
			MsSqlRepositoryDeleteTest();
		}

		static void MsSqlRepositoryAddTest()
		{
			var repo = new MsSqlRepository<TestAggregateRoot>("Server=localhost\\SQLEXPRESS;Initial Catalog=StateStoreTest;Integrated Security = true");

			Guid id = _rootId;

			var r = new TestAggregateRoot(id, new TestAggregateRootState(Random.Shared.Next(100)));

			repo.Add(r, _context).Wait();
		}

		static void MsSqlRepositoryGetTest()
		{
			var repo = new MsSqlRepository<TestAggregateRoot>("Server=localhost\\SQLEXPRESS;Initial Catalog=StateStoreTest;Integrated Security = true");

			Guid id = _rootId;

			var r = repo.Get(id).Result;

			if (r == null)
			{
				throw new Exception();
			}
		}

		static void MsSqlRepositoryUpdateTest()
		{
			var repo = new MsSqlRepository<TestAggregateRoot>("Server=localhost\\SQLEXPRESS;Initial Catalog=StateStoreTest;Integrated Security = true");

			Guid id = _rootId;

			var r = repo.Get(id).Result;
			if (r == null)
			{
				throw new Exception();
			}

			r.SetNumber(Random.Shared.Next(100));

			repo.Update(r, _context).Wait();
		}

		static void MsSqlRepositoryDeleteTest()
		{
			var repo = new MsSqlRepository<TestAggregateRoot>("Server=localhost\\SQLEXPRESS;Initial Catalog=StateStoreTest;Integrated Security = true");

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