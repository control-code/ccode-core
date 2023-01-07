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
		private readonly SortedList<Guid, object> _states = new SortedList<Guid, object>();

		public void Add(Guid id, object state)
		{
			_states.Add(id, state);
		}

		public void Add(Guid id, Guid rootId, object state)
		{
			throw new NotImplementedException();
		}

		public void Add(Guid id, Guid rootId, Guid? parentId, object state)
		{
			throw new NotImplementedException();
		}

		public Task Apply(Context context)
		{
			throw new NotImplementedException();
		}

		public Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			throw new NotImplementedException();
		}

		public void Delete<TState>(Guid id)
		{
			throw new NotImplementedException();
		}

		public void Delete(Type stateType, Guid id)
		{
			throw new NotImplementedException();
		}

		public void DeleteByRoot<TState>(Guid rootId)
		{
			throw new NotImplementedException();
		}

		public void DeleteByRoot(Type stateType, Guid rootId)
		{
			throw new NotImplementedException();
		}

		public void DeleteWithSubentities<TState>(Guid rootId)
		{
			throw new NotImplementedException();
		}

		public void DeleteWithSubentities(Type stateType, Guid rootId)
		{
			throw new NotImplementedException();
		}

		public Task<object?> Get<TState>(Guid id)
		{
			throw new NotImplementedException();
		}

		public Task<object?> Get(Type stateType, Guid id)
		{
			throw new NotImplementedException();
		}

		public Task<EntityData[]> GetByRoot<TState>(Guid rootId)
		{
			throw new NotImplementedException();
		}

		public Task<EntityData[]> GetByRoot(Type stateType, Guid rootId)
		{
			throw new NotImplementedException();
		}

		public void Update(Guid id, object state)
		{
			throw new NotImplementedException();
		}
	}
}
