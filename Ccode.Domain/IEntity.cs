namespace Ccode.Domain
{
	public interface IEntity<TState>
	{
		Guid Id { get; }

		TState State { get; }
	}
}