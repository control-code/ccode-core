using Ccode.Domain;

namespace Ccode.Adapters.Repository
{

	public interface IRepository<T, TState> where T : class, IAggregateRoot<TState>
	{
		Task Add(T entity, Context context);
		Task Update(T entity, Context context);
		Task Delete(T entity, Context context);

		Task<T?> Get(Guid id);
	}
}
