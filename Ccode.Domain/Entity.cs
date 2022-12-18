namespace Ccode.Domain
{
	public class Entity<TState> : IEntity<TState>
	{
		private Tracker _tracker;

		private TState _state;
		private Guid? _parentId;

		public Guid Id { get; }

		public TState State
		{
			get => _state;
			protected set
			{
				_state = value;
				_tracker.AddStateEvent(new StateEvent(Id, _parentId, StateEventOperation.Update, _state));
			}
		}

		public object StateObject => _state;

		public Entity(Tracker tracker, Guid id, Guid? parentId, TState state)
		{
			_tracker = tracker;
			_state = state;
			Id = id;
			_parentId = parentId;
		}

		public Entity(Tracker tracker, Guid id, TState state) : this(tracker, id, null, state)
		{
		}

		protected void AddEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			_tracker.AddStateEvent(new StateEvent(entity.Id, _parentId, StateEventOperation.Add, entity.State));
		}

		protected void DeleteEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			_tracker.AddStateEvent(new StateEvent(entity.Id, _parentId, StateEventOperation.Delete, entity.State));
		}
	}
}
