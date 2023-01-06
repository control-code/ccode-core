namespace Ccode.Domain.Entities
{
    public class ImmutableEntity<TState> : IEntity<TState>
    {
        public Guid Id { get; }

        public TState State { get; }

        public object StateObject { get; }

        public ImmutableEntity(Guid id, TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Id = id;
            State = state;
            StateObject = state;
        }
    }
}
