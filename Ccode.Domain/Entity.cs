#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.

namespace Ccode.Domain
{
	public class Entity<TState> : EntityBase, IEntity<TState>
	{
		private TState _state;

		protected EntityBase? Parent { get; }

		public TState State
		{
			get => _state;
			protected set
			{
				_state = value;
				Tracker.AddStateEvent(new StateEvent(Id, Parent?.Id, StateEventOperation.Update, _state));
			}
		}

		public override object StateObject => _state;

		public Entity(EntityBase parent, Guid id, TState state) : base(id, parent.Tracker)
		{
			Parent = parent;
			_state = state;
		}

		protected internal Entity(Tracker tracker, Guid id, TState state) : base(id, tracker)
		{
			Parent = null;
			_state = state;
		}

		protected void AddEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			Tracker.AddStateEvent(new StateEvent(entity.Id, Id, StateEventOperation.Add, entity.State));
		}

		protected void DeleteEntity<TEntityState>(IEntity<TEntityState> entity)
		{
			Tracker.AddStateEvent(new StateEvent(entity.Id, Id, StateEventOperation.Delete, entity.State));
		}
	}
}
