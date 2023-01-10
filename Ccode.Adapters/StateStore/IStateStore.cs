using Ccode.Domain;

namespace Ccode.Adapters.StateStore
{
    public interface IStateStore
	{
		Task<object?> Get<TState>(Guid id);
		Task<object?> Get(Type stateType, Guid id);
		Task<EntityData[]> GetByRoot<TState>(Guid rootId);
		Task<EntityData[]> GetByRoot(Type stateType, Guid rootId);

		Task Add(Guid id, object state, Context context);
		Task Add(Guid id, Guid rootId, object state, Context context);
		Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context);
		Task Update(Guid id, object state, Context context);
		Task Delete<TState>(Guid id, Context context);
		Task Delete(Type stateType, Guid id, Context context);
		Task DeleteByRoot<TState>(Guid rootId, Context context);
		Task DeleteByRoot(Type stateType, Guid rootId, Context context);
		Task DeleteWithSubstates<TState>(Guid rootId, Context context);
		Task DeleteWithSubstates(Type stateType, Guid rootId, Context context);

		Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context);
	}
}
