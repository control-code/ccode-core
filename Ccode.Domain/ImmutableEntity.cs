namespace Ccode.Domain
{
	public class ImmutableEntity<TState> : IEntity<TState>
	{
		public Guid Id { get; }

		public TState State { get; }

		public ImmutableEntity(Guid id, TState state)
		{
			Id = id;
			State = state;
		}
	}
}
