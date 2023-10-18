namespace Ccode.Contracts.StateStoreAdapter
{
	public class RootCannotBeDeletedException : Exception
	{
		public RootCannotBeDeletedException()
		{ }

		public RootCannotBeDeletedException(string? message) : base(message)
		{ }

		public RootCannotBeDeletedException(string? message, Exception? innerException) : base(message, innerException)
		{ }
	}
}
