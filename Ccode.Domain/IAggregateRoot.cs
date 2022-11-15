using System;

namespace Ccode.Domain
{
	public interface IAggregateRoot<TState> : IEntity<TState>
	{
		IEnumerable<StateEvent> GetStateEvents();
	}
}
