using Microsoft.Extensions.Hosting;
using Ccode.Contracts.StateEventAdapter;
using Ccode.Contracts.StateStoreAdapter;
using Ccode.Services.Identity;

namespace Ccode.Services.Users
{
	public record UserState(string Name);

	public class UsersService: IHostedService
	{
		private readonly IStateStoreAdapter _store;
		private readonly IStateEventAdapter _event;

		public UsersService(IStateStoreAdapter store, IStateEventAdapter @event)
		{
			_store = store;
			_event = @event;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var eventNumber = await _store.GetMaxEventNumber<UserState>();
			await _event.Subscribe<IdentityState>(eventNumber, ProcessIdentityEvent);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return _event.Unsubscribe<IdentityState>(ProcessIdentityEvent);
		}

		private Task ProcessIdentityEvent(StateStoreEvent ev)
		{
			var identityState = (IdentityState)ev.State;
			var state = new UserState(identityState.UserName);
			var uid = Guid.NewGuid();
			return _store.AddRoot(uid, state, new Domain.Context(Guid.Empty, Guid.Empty, ev.EventNumber));
		}
	}
}
