namespace Ccode.Domain
{
    public interface IAggregateRootBase: IEntityBase
	{
		bool HasEvents { get; }
		IEnumerable<StateEvent> GetStateEvents();
	}
}
