namespace Ccode.AdaptersImpl.Repository.MsSql
{
	public interface IStateStore
	{
		Task<EntityData[]> GetByRoot(Guid rootId);
		Task DeleteByRoot(Guid rootId);
	}
}
