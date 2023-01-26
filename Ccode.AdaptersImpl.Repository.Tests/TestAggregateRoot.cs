using Ccode.Domain.Entities;

namespace Ccode.AdaptersImpl.Repository.Tests
{
	public class TestAggregateRoot : AggregateRoot<TestAggregateRootState>
	{
		private List<TestSubentity> _subentities = new List<TestSubentity>();

		public TestAggregateRoot(Guid id, TestAggregateRootState state, StateInfo[] subentityStates) : base(id, state)
		{
			foreach(var sdata in subentityStates)
			{
				var subentity = new TestSubentity(this, sdata.Id, sdata.State);
				_subentities.Add(subentity);
			}
		}

		public void SetNumber(int newNumber)
		{
			State = State with { Number = newNumber };
		}

		public void AddTextItem(string text)
		{
			var item = new TestSubentity(this, Guid.NewGuid(), text);
			AddEntity(item);
		}
	}
}