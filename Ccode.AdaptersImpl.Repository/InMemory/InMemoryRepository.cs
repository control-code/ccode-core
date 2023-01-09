using System.Collections.Immutable;
using System.Collections.Concurrent;
using Ccode.Domain;
using Ccode.Adapters.Repository;

namespace Ccode.AdaptersImpl.Repository.InMemory
{
    public class InMemoryRepository<T, TState> : IRepository<T> where T : class, IAggregateRoot<TState>
	{
		private readonly ConcurrentQueue<StateEvent> _stateEvents = new ConcurrentQueue<StateEvent>();
		private readonly ConcurrentDictionary<Guid, T> _entities = new ConcurrentDictionary<Guid, T>();
		private readonly ConcurrentDictionary<Guid, TState> _entityStates = new ConcurrentDictionary<Guid, TState>();

		public ICollection<StateEvent> StateEvents => _stateEvents.ToImmutableList();
		public ICollection<TState> EntityStates => _entityStates.Values.ToImmutableList();

		public Task Add(T root, Context context)
		{
			if (root.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			var e = new StateEvent(root.Id, null, StateEventOperation.Add, root.State);
			_stateEvents.Enqueue(e);
			_entityStates[root.Id] = root.State;
			_entities[root.Id] = root;
			return Task.CompletedTask;
		}

		public Task Update(T root, Context context)
		{
			if (root.State == null)
			{
				throw new NullReferenceException("entity.State");
			}

			var events = root.GetStateEvents();

			foreach(var e in events)
			{
				_stateEvents.Enqueue(e);
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

			var e = new StateEvent(root.Id, null, StateEventOperation.Delete, root.State);
			_stateEvents.Enqueue(e);

			TState? state;
			_entityStates.TryRemove(root.Id, out state);

			T? entity;
			_entities.TryRemove(root.Id, out entity);

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
