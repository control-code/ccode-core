using System.Runtime.Serialization;

namespace Ccode.Adapters.StateStore
{
	public class RootCannotBeDeleted : Exception
	{
		public RootCannotBeDeleted()
		{ }

		public RootCannotBeDeleted(string? message) : base(message)
		{ }

		public RootCannotBeDeleted(string? message, Exception? innerException) : base(message, innerException)
		{ }

		protected RootCannotBeDeleted(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }
	}
}
