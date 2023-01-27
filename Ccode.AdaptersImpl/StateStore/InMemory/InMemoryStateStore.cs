using System.Collections.Concurrent;
using Ccode.Domain;
using Ccode.Domain.Entities;
using Ccode.Adapters.StateStore;
using System.ComponentModel;

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

		private class RootRecord
		{
			public SortedList<Guid, StateRecord> Substates { get; }

			public StateRecord StateRecord { get; }

			public RootRecord(Guid id, object state)
			{
				Substates = new();
				StateRecord = new StateRecord(id, id, null, state);
			}

			public StateRecord Add(Guid id, Guid? parentId, object state)
			{
				var record = new StateRecord(id, StateRecord.Id, parentId, state);
				Substates.Add(id, record);
				return record;
			}

			public void Delete(Guid id)
			{
				Substates.Remove(id);
			}

			public StateInfo[] GetStateInfos()
			{
				return Substates.Values.Select(r => new StateInfo(r.Id, r.RootId, r.ParentId, r.State)).ToArray();
			}
		}

		private readonly ConcurrentDictionary<Guid, RootRecord> _roots = new();
		private readonly ConcurrentDictionary<Guid, StateRecord> _index = new();

		public Task<object?> Get<TState>(Guid id)
		{
			return Get(typeof(TState), id);
		}

		public Task<object?> Get(Type stateType, Guid id)
		{
			
			return _index.TryGetValue(id, out var stateRecord) ? Task.FromResult<object?>(stateRecord.State) : Task.FromResult<object?>(null);
		}

		public Task<States?> GetByRoot<TState>(Guid rootId)
		{
			return GetByRoot(typeof(TState), rootId);
		}

		public Task<States?> GetByRoot(Type stateType, Guid rootId)
		{
			if (_roots.TryGetValue(rootId, out var root))
			{
				lock (root)
				{
					return Task.FromResult<States?>(new States(root.StateRecord.State, root.GetStateInfos()));
				}
			}
			
			return Task.FromResult<States?>(null);
		}

		public Task Add(Guid id, Guid rootId, object state, Context context)
		{
			return Add(id, rootId, null, state, context);
		}

		public Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context)
		{
			if (id == rootId)
			{
				throw new ArgumentException("Substate id must not be equal root id");
			}

			if (_roots.TryGetValue(rootId, out var root))
			{
				lock (root)
				{
					_index[id] = root.Add(id, parentId, state);					
				}
				return Task.CompletedTask;
			}

			throw new IdNotFoundException();
		}

		private void Add(RootRecord root, Guid id, Guid? parentId, object state, Context context)
		{
			if (id == root.StateRecord.Id)
			{
				throw new ArgumentException("Substate id must not be equal root id");
			}

			_index[id] = root.Add(id, parentId, state);
		}

		public Task Update(Guid id, object state, Context context)
		{
			if (_index.TryGetValue(id, out var item))
			{
				item.State = state;
				return Task.CompletedTask;
			}

			throw new IdNotFoundException();
		}

		private void Update(RootRecord root, Guid id, object state, Context context)
		{
			if (id == root.StateRecord.Id)
			{
				root.StateRecord.State = state;
			}
			else
			{
				root.Substates[id].State = state;
			}
		}

		public Task Delete<TState>(Guid id, Context context)
		{
			return Delete(typeof(TState), id, context);
		}

		public Task Delete(Type stateType, Guid id, Context context)
		{
			if (_index.TryRemove(id, out var record))
			{
				if (record.Id == record.RootId)
				{
					throw new RootCannotBeDeleted();
				}

				if (_roots.TryGetValue(record.RootId, out var root))
				{
					lock (root)
					{
						root.Delete(id);
					}
				}
			}

			return Task.CompletedTask;
		}

		private void Delete(RootRecord root, Guid id, Context context)
		{
			if (id == root.StateRecord.Id)
			{
				throw new RootCannotBeDeleted();
			}

			root.Delete(id);
			_index.TryRemove(id, out _);
		}

		public Task AddRoot(Guid id, object state, Context context)
		{
			var root = new RootRecord(id, state);

			if (!_roots.TryAdd(id, root))
			{
				throw new IdAlreadyExistsException();
			}

			_index[id] = root.StateRecord;

			return Task.CompletedTask;
		}

		public Task AddRoot(Guid id, object state, IEnumerable<StateEvent> events, Context context)
		{
			var root = new RootRecord(id, state);

			ApplyEvents(root, events, context);

			if (!_roots.TryAdd(id, root))
			{
				throw new IdAlreadyExistsException();
			}

			_index[id] = root.StateRecord;

			return Task.CompletedTask;
		}

		public Task DeleteRoot<TState>(Guid rootId, Context context)
		{
			return DeleteRoot(typeof(TState), rootId, context);
		}

		public Task DeleteRoot(Type stateType, Guid rootId, Context context)
		{
			if (_index.TryRemove(rootId, out var record))
			{
				if (record.Id != record.RootId)
				{
					throw new ArgumentException($"{nameof(rootId)} must be root id");
				}

				if (_roots.TryRemove(record.RootId, out var root))
				{
					lock (root)
					{
						foreach(var subId in root.Substates.Keys)
						{
							_index.TryRemove(subId, out _);
						}
					}
				}
			}

			return Task.CompletedTask;
		}

		public Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			if (!_roots.TryGetValue(rootId, out var root))
			{
				throw new IdNotFoundException();
			}
				
			lock(root)
			{
				ApplyEvents(root, events, context);
			}

			return Task.CompletedTask;
		}

		private void ApplyEvents(RootRecord root, IEnumerable<StateEvent> events, Context context)
		{
			foreach (var ev in events)
			{
				switch (ev.Operation)
				{
					case StateEventOperation.Add:
						Add(root, ev.EntityId, ev.ParentId, ev.State, context);
						break;
					case StateEventOperation.Update:
						Update(root, ev.EntityId, ev.State, context);
						break;
					case StateEventOperation.Delete:
						Delete(root, ev.EntityId, context);
						break;
				}
			}
		}
	}
}
