﻿using Ccode.Domain;
using Ccode.Domain.Entities;
using Ccode.Adapters.Repository;
using Ccode.Adapters.StateStore;

namespace Ccode.AdaptersImpl.Repository
{
	public class Repository<T, TState> : IRepository<T> where T : class, IAggregateRoot<TState>, IAggregateRootBase
	{
		private readonly IStateStore _store;

		private readonly EntityFactory<T, TState> _factory = new EntityFactory<T, TState>();

		public Repository(IStateStore store) 
		{
			_store = store;
		}

		public async Task<T?> Get(Guid id)
		{
			var states = await _store.GetByRoot<TState>(id);

			if (states == null)
			{
				return null;
			}

			return _factory.Create(id, (TState)states.RootState, states.Substates);
		}

		public async Task Add(T root, Context context)
		{
			var events = root.GetStateEvents();
			var addEvent = new StateEvent(root.Id, null, StateEventOperation.Add, root.StateObject);
			await _store.Apply(root.Id, events.Prepend(addEvent), context);
		}

		public Task Update(T root, Context context)
		{
			var events = root.GetStateEvents();
			return _store.Apply(root.Id, events, context);
		}

		public Task Delete(T root, Context context)
		{
			return _store.DeleteWithSubstates<TState>(root.Id, context);
		}
	}
}
