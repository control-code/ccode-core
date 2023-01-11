using Ccode.Domain.Entities;
using Ccode.AdaptersImpl.UnitTests;

namespace Ccode.AdaptersImpl.Repository.UnitTests
{
	public class TestAggregateRoot : AggregateRoot<TestRootEntityState>
	{
		public TestAggregateRoot(Guid id, TestRootEntityState state) : base(id, state)
		{ }

		public TestAggregateRoot(Guid id, int number, string text) : base(id, new TestRootEntityState(number, text))
		{ }

		public void SetText(string newText)
		{
			State = State with { Text = newText };
		}

		public void AddSubentity(double value)
		{
			var e = new TestSubentity(this, value);
			AddEntity(e);
		}
	}
}
