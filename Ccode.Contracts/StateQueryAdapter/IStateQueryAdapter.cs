namespace Ccode.Contracts.StateQueryAdapter
{
	public interface IStateQueryAdapter
	{
		Task<IEnumerable<EntityState<TState>>> GetAll<TState>() where TState : class;
		Task<IEnumerable<EntityState<TState>>> Get<TState>(string fieldName, string fieldValue) where TState : class;
		Task<IEnumerable<Guid>> GetUids<TState>(string fieldName, string fieldValue) where TState : class;
	}
}
