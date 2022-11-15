namespace Ccode.Domain
{
	public class Entity<TState> : IEntity<TState>
	{
		private Tracker _tracker;

		private TState _state;

		public Guid Id { get; }

		public TState State
		{
			get => _state;
			private set
			{
				_state = value;
				_tracker.AddStateEvent(new StateEvent(Id, StateEventOperation.Update, _state));
			}
		}

		public Entity(Tracker tracker, Guid id, TState state)
		{
			_tracker = tracker;
			_state = state;
			Id = id;
		}

		protected void AddEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			_tracker.AddStateEvent(new StateEvent(entity.Id, StateEventOperation.Add, entity.State));
		}

		protected void DeleteEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			_tracker.AddStateEvent(new StateEvent(entity.Id, StateEventOperation.Delete, entity.State));
		}
	}
}
