namespace Ccode.Domain
{
    public record StateEvent(Guid EntityId, Guid? ParentId, StateEventOperation Operation, object State);
}
