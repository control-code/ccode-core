namespace Ccode.Domain.Entities
{
    public class Tracker
    {
        private List<StateEvent> _stateEvents = new List<StateEvent>();

        internal IEnumerable<StateEvent> GetStateEvents()
        {
            var arr = _stateEvents.ToArray();
            _stateEvents.Clear();
            return arr;
        }

        internal void AddStateEvent(StateEvent stateEvent)
        {
            _stateEvents.Add(stateEvent);
        }
    }
}
