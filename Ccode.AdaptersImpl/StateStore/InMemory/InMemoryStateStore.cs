using System.Collections.Concurrent;
using Ccode.Adapters.StateStore;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.StateStore.InMemory
{
	public class InMemoryStateStore : IStateStore
	{
		private class Item
		{
			
		}
		
		private readonly ConcurrentDictionary<Guid, List<EntityData>> _statesByRoot = new();
		private readonly ConcurrentDictionary<Guid, EntityData> _states = new();

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
					var array = list.ToArray();
					return Task.FromResult(array);
				}
			}
			
			return Task.FromResult(Array.Empty<EntityData>());
		}

		public Task Add(Guid id, object state, Context context)
		{
			var item = new EntityData(id, id, null, state);
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
			var item = new EntityData(id, rootId, parentId, state);
			if (!_states.TryAdd(id, item))
			{
				throw new IdAlreadyExistsException();
			}
			
			var list = _statesByRoot.GetOrAdd(rootId, new List<EntityData>());
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

		public Task DeleteWithSubentities<TState>(Guid rootId, Context context)
		{
			return DeleteWithSubentities(typeof(TState), rootId, context);
		}

		public Task DeleteWithSubentities(Type stateType, Guid rootId, Context context)
		{
			_statesByRoot.TryRemove(rootId, out _);
			_states.TryRemove(rootId, out _);
			return Task.CompletedTask;
		}

		public Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			throw new NotImplementedException();
		}
	}
}
