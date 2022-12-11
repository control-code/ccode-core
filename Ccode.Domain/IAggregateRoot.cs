using System;

namespace Ccode.Domain
{
	public interface IAggregateRoot<TState> : IAggregateRootBase, IEntity<TState>
	{
	}
}
