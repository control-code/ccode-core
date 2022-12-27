namespace Ccode.Domain
{
	public class AggregateRoot<TState> : Entity<TState>, IAggregateRoot<TState>
	{
		public AggregateRoot(Guid id, TState state) : base(new Tracker(), id, state)
		{ }

		public IEnumerable<StateEvent> GetStateEvents()
		{
			return Tracker.GetStateEvents();
		}
	}
}
