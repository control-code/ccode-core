using Ccode.Domain;

namespace Ccode.Contracts.StateStoreAdapter
{

	public interface IStateStoreAdapter
	{
		Task<TState> Get<TState>(Guid uid) where TState : class;

		Task AddRoot<TRootState>(Guid uid, TRootState state, Context context) where TRootState: class;

		Task DeleteRoot<TRootState>(Guid uid, Context context);

		/*
		Task<object?> Get(Type stateType, Guid id);
		Task AddRoot<TRootState>(Guid id, TRootState state, IEnumerable<StateEvent> events, Context context);
		Task DeleteRoot(Type rootStateType, Guid rootId, Context context);

		Task<States?> GetByRoot<TState>(Guid rootId);
		Task<States?> GetByRoot(Type stateType, Guid rootId);

		Task Add(Guid id, Guid rootId, object state, Context context);
		Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context);
		Task Update(Guid id, object state, Context context);
		Task Delete<TState>(Guid id, Context context);
		Task Delete(Type stateType, Guid id, Context context);

		Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context);
		*/
	}
}
