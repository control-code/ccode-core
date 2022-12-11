namespace Ccode.Domain
{
	public interface IEntityBase
	{
		Guid Id { get; }

		object StateObject { get; }
	}
}