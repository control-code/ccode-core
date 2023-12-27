using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ccode.Contracts.StateEventAdapter
{
	public enum StateStoreEventType
	{
		RootAdded = 0,
		RootDeleted = 1
	}

	public record StateStoreEvent(long EventNumber, StateStoreEventType EventType, Type AggregateStateType, Guid Uid, object State);

	public interface IStateEventAdapter
	{
		Task Subscribe<TRootState>(long lastProcessedEventNumber, Func<StateStoreEvent, Task> callback);
		Task Unsubscribe<TRootState>(Func<StateStoreEvent, Task> callback);
	}
}
