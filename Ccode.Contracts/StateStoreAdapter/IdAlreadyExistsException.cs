namespace Ccode.Contracts.StateStoreAdapter;

public class IdAlreadyExistsException : Exception
{
	public IdAlreadyExistsException()
	{ }

	public IdAlreadyExistsException(string? message) : base(message)
	{ }

	public IdAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
	{ }
}
