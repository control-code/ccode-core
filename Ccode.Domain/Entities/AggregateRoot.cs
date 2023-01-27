namespace Ccode.Domain.Entities
{
    public class AggregateRoot<TState> : Entity<TState>, IAggregateRoot<TState>
    {
		public bool HasEvents => Tracker.HasEvents;

		public AggregateRoot(Guid id, TState state) : base(new Tracker(), id, state)
        { }

        public IEnumerable<StateEvent> GetStateEvents()
        {
            return Tracker.GetStateEvents();
        }
    }
}
