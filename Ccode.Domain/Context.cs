namespace Ccode.Domain
{
	public record Context(Guid InitiatorId, Guid CorrelationId, long EventNumber);
}