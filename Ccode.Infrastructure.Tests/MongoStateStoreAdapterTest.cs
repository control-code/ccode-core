using Microsoft.Extensions.Options;
using Ccode.Domain;

namespace Ccode.Infrastructure.Tests
{
	public class MongoStateStoreAdapterTest
	{
		record State(int Number, string Name);

		[Fact]
		public void AddRootTest()
		{
			var config = new MongoStateStoreAdapter.MongoStateStoreAdapterConfig { ConnectionString = "mongodb://localhost:27017/stateStoreTest" };
			var options = Options.Create(config);
			var mongoStateStoreAdapter = new MongoStateStoreAdapter.MongoStateStoreAdapter(options);
			
			var stateA = new State(0, "Test");
			var context = new Context(Guid.Empty, Guid.NewGuid());
			var id = Guid.NewGuid();
			mongoStateStoreAdapter.AddRoot(id, stateA, context).Wait();
			var stateB = mongoStateStoreAdapter.Get<State>(id).Result;

			Assert.Equal(stateA, stateB);
		}
	}
}