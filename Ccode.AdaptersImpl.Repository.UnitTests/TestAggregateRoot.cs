using Ccode.Domain.Entities;
using Ccode.AdaptersImpl.UnitTests;
using Ccode.Adapters.StateStore;

namespace Ccode.AdaptersImpl.Repository.UnitTests
{
	public class TestAggregateRoot : AggregateRoot<TestRootEntityState>
	{
		private List<TestSubentity> _subentities = new();

		public int SubentityCount => _subentities.Count;

		public TestAggregateRoot(Guid id, TestRootEntityState state, EntityData[] subentities) : base(id, state)
		{ 
			foreach(var data in subentities.Where(d => d.ParentId == id))
			{
				var s = data.State as TestSubentityState;
				if (s != null)
				{
					var e = new TestSubentity(this, data.Id, (TestSubentityState)data.State);
					_subentities.Add(e);
				}
			}
		}

		public TestAggregateRoot(Guid id, int number, string text) : base(id, new TestRootEntityState(number, text))
		{ }

		public void SetText(string newText)
		{
			State = State with { Text = newText };
		}

		public Guid AddSubentity(double value)
		{
			var e = new TestSubentity(this, value);
			_subentities.Add(e);
			AddEntity(e);

			return e.Id;
		}

		public void DeleteFirstSubentity()
		{
			if (_subentities.Count > 0)
			{
				DeleteEntity(_subentities[0]);
				_subentities.RemoveAt(0);
			}
		}
	}
}
