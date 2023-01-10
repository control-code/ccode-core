using System.Collections.Concurrent;
using Ccode.Adapters.StateStore;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.StateStore.InMemory
{
	public class InMemoryStateStore : IStateStore
	{
		private class StateRecord
		{
			public StateRecord(Guid id, Guid rootId, Guid? parentId, object state)
			{
				Id = id;
				RootId = rootId;
				ParentId = parentId;
				State = state;
			}

			public Guid Id { get; }
			public Guid RootId { get; }
			public Guid? ParentId { get; }
			public object State { get; set; }
		}
		
		private readonly ConcurrentDictionary<Guid, List<StateRecord>> _statesByRoot = new();
		private readonly ConcurrentDictionary<Guid, StateRecord> _states = new();

		public Task<object?> Get<TState>(Guid id)
		{
			return Get(typeof(TState), id);
		}

		public Task<object?> Get(Type stateType, Guid id)
		{
			return _states.ContainsKey(id) ? Task.FromResult((object?)_states[id].State) : Task.FromResult((object?)null);
		}

		public Task<EntityData[]> GetByRoot<TState>(Guid rootId)
		{
			return GetByRoot(typeof(TState), rootId);
		}

		public Task<EntityData[]> GetByRoot(Type stateType, Guid rootId)
		{
			if (_statesByRoot.TryGetValue(rootId, out var list))
			{
				lock (list)
				{
					var array = list.Select(r => new EntityData(r.Id, r.RootId, r.ParentId, r.State)).ToArray();
					return Task.FromResult(array);
				}
			}
			
			return Task.FromResult(Array.Empty<EntityData>());
		}

		public Task Add(Guid id, object state, Context context)
		{
			var item = new StateRecord(id, id, null, state);
			if (!_states.TryAdd(id, item))
			{
				throw new IdAlreadyExistsException();
			}
			return Task.CompletedTask;
		}

		public Task Add(Guid id, Guid rootId, object state, Context context)
		{
			return Add(id, rootId, null, state, context);
		}

		public Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context)
		{
			var item = new StateRecord(id, rootId, parentId, state);
			if (!_states.TryAdd(id, item))
			{
				throw new IdAlreadyExistsException();
			}
			
			var list = _statesByRoot.GetOrAdd(rootId, new List<StateRecord>());
			lock (list)
			{
				list.Add(item);
			}
			
			return Task.CompletedTask;
		}

		public Task Update(Guid id, object state, Context context)
		{
			if (_states.TryGetValue(id, out var item))
			{
				item.State = state;
			}
			else
			{
				throw new IdNotFoundException();
			}
			
			return Task.CompletedTask;
		}

		public Task Delete<TState>(Guid id, Context context)
		{
			return Delete(typeof(TState), id, context);
		}

		public Task Delete(Type stateType, Guid id, Context context)
		{
			if (_states.TryRemove(id, out var item))
			{
				if (_statesByRoot.TryGetValue(item.RootId, out var list))
				{
					lock (list)
					{
						list.Remove(item);
					}
				}
			}

			return Task.CompletedTask;
		}

		public Task DeleteByRoot<TState>(Guid rootId, Context context)
		{
			return DeleteByRoot(typeof(TState), rootId, context);
		}

		public Task DeleteByRoot(Type stateType, Guid rootId, Context context)
		{
			if (_statesByRoot.TryGetValue(rootId, out var list))
			{
				lock (list)
				{
					var items = list.Where(i => i.State.GetType() == stateType);
					foreach (var item in items)
					{
						_states.Remove(item.Id, out _);
					}
					list.RemoveAll(i => i.State.GetType() == stateType);
				}
			}

			return Task.CompletedTask;
		}

		public Task DeleteWithSubstates<TState>(Guid rootId, Context context)
		{
			return DeleteWithSubstates(typeof(TState), rootId, context);
		}

		public Task DeleteWithSubstates(Type stateType, Guid rootId, Context context)
		{
			if (_statesByRoot.TryGetValue(rootId, out var list))
			{
				lock (list)
				{
					foreach (var item in list)
					{
						_states.Remove(item.Id, out _);
					}
				}

				_statesByRoot.TryRemove(rootId, out _);
			}

			_states.TryRemove(rootId, out _);

			return Task.CompletedTask;
		}

		public Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			foreach (var ev in events)
			{
				switch (ev.Operation)
				{
					case StateEventOperation.Add:
						Add(ev.EntityId, rootId, ev.ParentId, ev.State, context);
						break;
					case StateEventOperation.Update:
						Update(ev.EntityId, ev.State, context);
						break;
					case StateEventOperation.Delete:
						Delete(ev.State.GetType(), ev.EntityId, context);
						break;
				}
			}

			return Task.CompletedTask;
		}
	}
}
