using Microsoft.Extensions.Options;
using Ccode.Domain;
using Ccode.Contracts.StateEventAdapter;

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

			StateStoreEvent? ev = null;

			mongoStateStoreAdapter.Subscribe<State>(0, e =>
			{
				ev = e;
				return Task.CompletedTask;
			});
			
			var stateA = new State(0, "Test");
			var context = new Context(Guid.Empty, Guid.NewGuid(), 0);
			var id = Guid.NewGuid();
			mongoStateStoreAdapter.AddRoot(id, stateA, context).Wait();
			var stateB = mongoStateStoreAdapter.Get<State>(id).Result;

			mongoStateStoreAdapter.PollEvents().Wait();

			Assert.Equal(stateA, stateB);
			Assert.Equal(StateStoreEventType.RootAdded, ev?.EventType);
			Assert.Equal(id, ev?.Uid);
			Assert.Equal(stateA, ev?.State);
		}
	}
}