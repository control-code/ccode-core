namespace Ccode.Domain
{
	public class AggregateRoot<TState> : Tracker, IAggregateRoot<TState>
	{
		private TState _state;

		public Guid Id { get; }

		public TState State 
		{ 
			get => _state;
			protected set 
			{
				if (value == null)
				{
					throw new NullReferenceException("State");
				}

				_state = value;
				AddStateEvent(new StateEvent(Id, null, StateEventOperation.Update, _state));
			} 
		}

		public object StateObject => _state;

		public AggregateRoot(Guid id, TState state)
		{
			Id = id;
			_state = state;
		}

		public IEnumerable<StateEvent> GetStateEvents()
		{
			var arr = StateEvents.ToArray();
			StateEvents.Clear();
			return arr;
		}

		protected void AddEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			if (entity.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			AddStateEvent(new StateEvent(entity.Id, null, StateEventOperation.Add, entity.State));
		}

		protected void DeleteEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			if (entity.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			AddStateEvent(new StateEvent(entity.Id, null, StateEventOperation.Delete, entity.State));
		}
	}
}
