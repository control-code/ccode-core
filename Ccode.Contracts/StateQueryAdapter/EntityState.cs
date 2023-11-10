namespace Ccode.Contracts.StateQueryAdapter
{
	public record EntityState<TState>(Guid Uid, TState State);
}
