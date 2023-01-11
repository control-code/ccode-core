using Ccode.Domain.Entities;
using Ccode.AdaptersImpl.UnitTests;

namespace Ccode.AdaptersImpl.Repository.UnitTests
{
	public class TestSubentity : Entity<TestSubentityState>
	{
		public TestSubentity(EntityBase parent, double value) 
			: base(parent, Guid.NewGuid(), new TestSubentityState(value))
		{ }

		public TestSubentity(EntityBase parent, Guid id, TestSubentityState state) : base(parent, id, state)
		{ }
	}
}
