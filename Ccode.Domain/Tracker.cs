namespace Ccode.Domain
{
	public class Tracker
	{
		protected List<StateEvent> StateEvents = new List<StateEvent>();

		internal void AddStateEvent(StateEvent stateEvent)
		{
			StateEvents.Add(stateEvent);
		}
	}
}
