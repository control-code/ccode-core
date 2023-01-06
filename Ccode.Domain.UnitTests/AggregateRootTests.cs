using Ccode.Domain.Entities;

namespace Ccode.Domain.UnitTests
{
    public class AggregateRootTests
	{
		public record TestState(string Text);

		public class TestAggregateRoot : AggregateRoot<TestState>
		{
			public TestAggregateRoot(Guid id, TestState state) : base(id, state)
			{ }

			public TestAggregateRoot(Guid id) : base(id, new TestState(string.Empty))
			{ }

			public void UpdateText(string text)
			{
				State = State with { Text = text };
			}
		}

		[Fact]
		public void StateEvents()
		{
			var id = Guid.NewGuid();
			var text = "Test text";
			var a = new TestAggregateRoot(id);
			a.UpdateText(text);
			var events = a.GetStateEvents();
			Assert.Equal(id, events.First().EntityId);
			Assert.True(events.First().State is TestState);
			Assert.True(((TestState)(events.First().State)).Text == text);
		}
	}
}