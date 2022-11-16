using System.Reflection;
using System.Collections.Immutable;
using Ccode.Domain;
using Ccode.Adapters.Repository;

namespace Ccode.AdaptersImpl.Repository.InMemory
{
 	public class MemRepository<T, TState> : IRepository<T, TState> where T : class, IAggregateRoot<TState>
	{
		private List<StateEvent> _stateEvents = new List<StateEvent>();
		private SortedList<Guid, T> _entities = new SortedList<Guid, T>();
		private SortedList<Guid, TState> _entityStates = new SortedList<Guid, TState>();

		public ICollection<StateEvent> StateEvents { get { return _stateEvents.ToImmutableList(); } }
		public ICollection<TState> EntityStates { get { return _entityStates.Values.ToImmutableList(); } }

		public Task Add(T root, Context context)
		{
			if (root.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			var e = new StateEvent(root.Id, StateEventOperation.Add, root.State);
			_stateEvents.Add(e);
			_entityStates.Add(root.Id, root.State);
			_entities.Add(root.Id, root);
			return Task.CompletedTask;
		}

		public Task Update(T root, Context context)
		{
			if (root.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			var events = root.GetStateEvents();
			_stateEvents.AddRange(events);

			foreach(var e in events)
			{
				_entityStates[e.EntityId] = (TState)e.State;
			}

			return Task.CompletedTask;
		}

		public Task Delete(T root, Context context)
		{
			if (root.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			var e = new StateEvent(root.Id, StateEventOperation.Delete, root.State);
			_stateEvents.Add(e);
			_entityStates.Remove(root.Id);
			_entities.Remove(root.Id);
			return Task.CompletedTask;
		}

		public Task<T?> Get(Guid id)
		{
			if (!_entities.ContainsKey(id))
			{
				return Task.FromResult((T?)null);
			}

			return Task.FromResult((T?)_entities[id]);
		}
	}
}
