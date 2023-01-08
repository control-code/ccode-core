using Ccode.Adapters.StateStore;
using Ccode.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ccode.AdaptersImpl.StateStore.InMemory
{
	public class InMemoryStateStore : IStateStore
	{
		private readonly SortedList<Guid, List<EntityData>> _statesByRoot = new SortedList<Guid, List<EntityData>>();
		private readonly SortedList<Guid, List<EntityData>> _statesByParent = new SortedList<Guid, List<EntityData>>();
		private readonly SortedList<Guid, object> _states = new SortedList<Guid, object>();

		public Task<object?> Get<TState>(Guid id)
		{
			return Get(typeof(TState), id);
		}

		public Task<object?> Get(Type stateType, Guid id)
		{
			return _states.ContainsKey(id) ? Task.FromResult((object?)_states[id]) : Task.FromResult((object?)null);
		}

		public Task<EntityData[]> GetByRoot<TState>(Guid rootId)
		{
			return GetByRoot(typeof(TState), rootId);
		}

		public Task<EntityData[]> GetByRoot(Type stateType, Guid rootId)
		{
			return _statesByRoot.ContainsKey(rootId)
				? Task.FromResult(_statesByRoot[rootId].ToArray())
				: Task.FromResult(Array.Empty<EntityData>());
		}

		public Task Add(Guid id, object state, Context context)
		{
			_states.Add(id, state);
			return Task.CompletedTask;
		}

		public Task Add(Guid id, Guid rootId, object state, Context context)
		{
			throw new NotImplementedException();
		}

		public Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context)
		{
			throw new NotImplementedException();
		}

		public Task Update(Guid id, object state, Context context)
		{
			throw new NotImplementedException();
		}

		public Task Delete<TState>(Guid id, Context context)
		{
			throw new NotImplementedException();
		}

		public Task Delete(Type stateType, Guid id, Context context)
		{
			throw new NotImplementedException();
		}

		public Task DeleteByRoot<TState>(Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public Task DeleteByRoot(Type stateType, Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public Task DeleteWithSubentities<TState>(Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public Task DeleteWithSubentities(Type stateType, Guid rootId, Context context)
		{
			throw new NotImplementedException();
		}

		public Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			throw new NotImplementedException();
		}
	}
}
