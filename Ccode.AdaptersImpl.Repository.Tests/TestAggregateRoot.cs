using Ccode.Domain;

namespace Ccode.AdaptersImpl.Repository.Tests
{
	public class TestAggregateRoot : AggregateRoot<TestAggregateRootState>
	{
		public TestAggregateRoot(Guid id, TestAggregateRootState state) : base(id, state)
		{
		}

		public void SetNumber(int newNumber)
		{
			State = State with { Number = newNumber };
		}
	}
}