using System.Reflection;
using System.Collections.Immutable;
using Ccode.Domain;
using Ccode.Adapters.Repository;

namespace Ccode.AdaptersImpl.Repository.InMemory
{
 	public class MemRepository<T, TState> : IRepository<T, TState> where T : class, IAggregateRoot<TState>
	{
		private List<StateEvent> _stateEvents = new List<StateEvent>();
		private SortedList<Guid, TState> _entityStates = new SortedList<Guid, TState>();

		public ICollection<StateEvent> StateEvents { get { return _stateEvents.ToImmutableList(); } }

		public Task Add(T root, Context context)
		{
			if (root.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			var e = new StateEvent(root.Id, StateEventOperation.Add, root.State);
			_stateEvents.Add(e);
			_entityStates.Add(root.Id, root.State);
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

			var e = new StateEvent(root.Id, StateEventOperation.Add, root.State);
			_stateEvents.Add(e);
			_entityStates.Remove(root.Id);
			return Task.CompletedTask;
		}

		public Task<T?> Get(Guid id)
		{
			if (!_entityStates.ContainsKey(id))
			{
				return Task.FromResult((T?)null);
			}

			var state = _entityStates[id];

			if (state == null)
			{
				return Task.FromResult((T?)null);
			}

			ConstructorInfo? ctor = typeof(T).GetConstructor(new[] { typeof(Guid), typeof(TState) });
			object? instance = ctor?.Invoke(new object[] { id, state });

			return Task.FromResult((T?)instance);
		}
	}
}
