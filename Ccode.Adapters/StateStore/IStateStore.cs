using Ccode.Domain;

namespace Ccode.Adapters.StateStore
{

	public interface IStateStore
	{
		Task<object?> Get<TState>(Guid id);
		Task<object?> Get(Type stateType, Guid id);
		Task<States?> GetByRoot<TState>(Guid rootId);
		Task<States?> GetByRoot(Type stateType, Guid rootId);

		Task AddRoot(Guid id, object state, Context context);
		Task AddRoot(Guid id, object state, IEnumerable<StateEvent> events, Context context);
		Task DeleteRoot<TRootState>(Guid rootId, Context context);
		Task DeleteRoot(Type rootStateType, Guid rootId, Context context);

		Task Add(Guid id, Guid rootId, object state, Context context);
		Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context);
		Task Update(Guid id, object state, Context context);
		Task Delete<TState>(Guid id, Context context);
		Task Delete(Type stateType, Guid id, Context context);

		Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context);
	}
}
