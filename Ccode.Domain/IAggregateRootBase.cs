namespace Ccode.Domain
{
    public interface IAggregateRootBase: IEntityBase
	{
		IEnumerable<StateEvent> GetStateEvents();
	}
}
