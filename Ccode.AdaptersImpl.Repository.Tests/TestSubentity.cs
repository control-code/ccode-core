using Ccode.Domain.Entities;

namespace Ccode.AdaptersImpl.Repository.Tests
{
    public class TestSubentity : Entity<TestSubentityState>
	{
		public TestSubentity(TestAggregateRoot root, Guid id, object state) 
			: base(root, id, (TestSubentityState)state)
		{ }

		public TestSubentity(TestAggregateRoot root, Guid id, string text) 
			: base(root, id, new TestSubentityState(text))
		{ }

		public void SetText(string newText)
		{
			State = State with { Text = newText };
		}
	}
}