namespace Ccode.Domain
{
	public record StateEvent(Guid EntityId, StateEventOperation Operation, object State);
}
