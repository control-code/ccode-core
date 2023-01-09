namespace Ccode.Adapters.StateStore;

public class IdNotFoundException : Exception
{
	public IdNotFoundException()
	{ }

	public IdNotFoundException(string? message) : base(message)
	{ }

	public IdNotFoundException(string? message, Exception? innerException) : base(message, innerException)
	{ }
}