using Ccode.Domain;

namespace Ccode.Adapters.Repository
{

	public interface IRepository<T> where T : class, IAggregateRootBase
	{
		Task Add(T root, Context context);
		Task Update(T root, Context context);
		Task Delete(T root, Context context);

		Task<T?> Get(Guid id);
	}
}
