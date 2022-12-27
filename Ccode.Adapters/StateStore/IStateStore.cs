using Ccode.Domain;

namespace Ccode.Adapters.StateStore
{
	public interface IStateStore
	{
		Task<object?> Get<TState>(Guid id);
		Task<object?> Get(Type stateType, Guid id);
		Task<EntityData[]> GetByRoot<TState>(Guid rootId);
		Task<EntityData[]> GetByRoot(Type stateType, Guid rootId);

		void Add(Guid id, object state);
		void Add(Guid id, Guid rootId, object state);
		void Add(Guid id, Guid rootId, Guid? parentId, object state);
		void Update(Guid id, object state);
		void Delete<TState>(Guid id);
		void Delete(Type stateType, Guid id);
		void DeleteByRoot<TState>(Guid rootId);
		void DeleteByRoot(Type stateType, Guid rootId);
		void DeleteWithSubentities<TState>(Guid rootId);
		void DeleteWithSubentities(Type stateType, Guid rootId);

		Task Apply(Context context);
		Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context);
	}
}
