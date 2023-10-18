using Ccode.Domain.Entities;

namespace Ccode.Contracts.StateStoreAdapter
{
	public class States
	{
		public States(object rootState, StateInfo[] substates)
		{
			RootState = rootState;
			Substates = substates;
		}

		public object RootState { get; }
		public StateInfo[] Substates { get; }
	}
}
