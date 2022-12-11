namespace Ccode.Domain
{
	public interface IEntity<TState>: IEntityBase
	{
		TState State { get; }
	}
}