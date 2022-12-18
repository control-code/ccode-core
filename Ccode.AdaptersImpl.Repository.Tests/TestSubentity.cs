using Ccode.Domain;

namespace Ccode.AdaptersImpl.Repository.Tests
{
	public class TestSubentity : Entity<TestSubentityState>
	{
		public TestSubentity(Tracker tracker, Guid id, object state) : base(tracker, id, (TestSubentityState)state)
		{}

		public TestSubentity(Tracker tracker, Guid id, string text) : base(tracker, id, new TestSubentityState(text))
		{ }

		public void SetText(string newText)
		{
			State = State with { Text = newText };
		}
	}
}