using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Hosting;
using Dapper;
using Ccode.Adapters.StateStore;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.StateStore.MsSql
{
    internal abstract class StoreStateEvent
	{
		protected readonly MsSqlEntityStateStore Store;
		protected readonly Guid Id;

		public StoreStateEvent(MsSqlEntityStateStore store, Guid id)
		{
			Store = store;
			Id = id;
		}

		public abstract Task Apply(Context context, SqlConnection connection, IDbTransaction transaction);
	}

	internal class StoreAddStateEvent: StoreStateEvent
	{
		private readonly Guid _rootId;
		private readonly Guid? _parentId;
		private readonly object _state;

		public StoreAddStateEvent(MsSqlEntityStateStore store, Guid id, Guid rootId, Guid? parentId, object state) 
			: base(store, id)
		{
			_rootId = rootId;
			_parentId = parentId;
			_state = state;
		}

		public override Task Apply(Context context, SqlConnection connection, IDbTransaction transaction)
		{
			return Store.Add(Id, _rootId, _parentId, _state, context, connection, transaction);
		}
	}

	internal class StoreUpdateStateEvent : StoreStateEvent
	{
		private readonly object _state;

		public StoreUpdateStateEvent(MsSqlEntityStateStore store, Guid id, object state)
			: base(store, id)
		{
			_state = state;
		}

		public override Task Apply(Context context, SqlConnection connection, IDbTransaction transaction)
		{
			return Store.Update(Id, _state, context, connection, transaction);
		}
	}

	internal class StoreDeleteStateEvent : StoreStateEvent
	{
		public StoreDeleteStateEvent(MsSqlEntityStateStore store, Guid id)
			: base(store, id)
		{ }

		public override Task Apply(Context context, SqlConnection connection, IDbTransaction transaction)
		{
			return Store.Delete(Id, context, connection, transaction);
		}
	}

	internal class StoreDeleteRootStateEvent : StoreStateEvent
	{
		public StoreDeleteRootStateEvent(MsSqlEntityStateStore store, Guid id)
			: base(store, id)
		{ }

		public override Task Apply(Context context, SqlConnection connection, IDbTransaction transaction)
		{
			return Store.DeleteByRoot(Id, context, connection, transaction);
		}
	}

	public class MsSqlStateStore : IStateStore, IHostedService
	{
		private readonly string _connectionString;
		private readonly ConcurrentDictionary<string, MsSqlEntityStateStore> _entityTypeStores = new ();
		private readonly List<MsSqlEntityStateStore> _subentityStores = new ();
		private readonly List<StoreStateEvent> _stateEvents = new ();
		private readonly SortedList<string, List<Type>> _subentityTypes = new ();

		public MsSqlStateStore(string connectionString)
		{
			_connectionString = connectionString;
		}

		public Task<object?> Get<TState>(Guid id)
		{
			var store = GetStoreByType(typeof(TState));
			return store.Get(id);
		}

		public Task<object?> Get(Type stateType, Guid id)
		{
			var store = GetStoreByType(stateType);
			return store.Get(id);
		}

		public Task<EntityData[]> GetByRoot<TState>(Guid rootId)
		{
			var store = GetStoreByType(typeof(TState));
			return store.GetByRoot(rootId);
		}

		public Task<EntityData[]> GetByRoot(Type stateType, Guid rootId)
		{
			var store = GetStoreByType(stateType);
			return store.GetByRoot(rootId);
		}

		public Task Add(Guid id, object state, Context context)
		{
			return Add(id, id, null, state, context);
		}

		public Task Add(Guid id, Guid rootId, object state, Context context)
		{
			return Add(id, rootId, null, state, context);
		}

		public async Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context)
		{
			var store = GetStoreByType(state.GetType());
			await using var connection = new SqlConnection(_connectionString);
			await store.Add(id, rootId, parentId, state, context, connection);
		}

		public async Task Update(Guid id, object state, Context context)
		{
			var store = GetStoreByType(state.GetType());
			await using var connection = new SqlConnection(_connectionString);
			await store.Update(id, state, context, connection);
		}

		public Task Delete<TState>(Guid id, Context context)
		{
			return Delete(typeof(TState), id, context);
		}

		public async Task Delete(Type stateType, Guid id, Context context)
		{
			var store = GetStoreByType(stateType);
			await using var connection = new SqlConnection(_connectionString);
			await store.Delete(id, context, connection);
		}

		public Task DeleteByRoot<TState>(Guid rootId, Context context)
		{
			return DeleteByRoot(typeof(TState), rootId, context);
		}

		public async Task DeleteByRoot(Type stateType, Guid rootId, Context context)
		{
			var store = GetStoreByType(stateType);
			await using var connection = new SqlConnection(_connectionString);
			await store.DeleteByRoot(rootId, context, connection);
		}

		public Task DeleteWithSubstates<TState>(Guid rootId, Context context)
		{
			var stateType = typeof(TState);
			return DeleteWithSubstates(stateType, rootId, context);
		}

		public async Task DeleteWithSubstates(Type stateType, Guid rootId, Context context)
		{
			await using var connection = new SqlConnection(_connectionString);
			await connection.OpenAsync();
			await using var transaction = await connection.BeginTransactionAsync();

			var substores = GetStoresByRootType(stateType);
			foreach (var substore in substores)
			{
				await substore.DeleteByRoot(rootId, context, connection, transaction); // TODO: make await composition
			}

			var store = GetStoreByType(stateType);
			await store.DeleteByRoot(rootId, context, connection,transaction);
		}

		public void Add(Guid id, object state)
		{
			var store = GetStoreByType(state.GetType());
			_stateEvents.Add(new StoreAddStateEvent(store, id, id, null, state));
		}

		public void Add(Guid id, Guid rootId, object state)
		{
			var store = GetStoreByType(state.GetType());
			_stateEvents.Add(new StoreAddStateEvent(store, id, rootId, rootId, state));
		}

		public void Add(Guid id, Guid rootId, Guid? parentId, object state)
		{
			var store = GetStoreByType(state.GetType());
			_stateEvents.Add(new StoreAddStateEvent(store, id, rootId, parentId, state));
		}

		public void Update(Guid id, object state)
		{
			var store = GetStoreByType(state.GetType());
			_stateEvents.Add(new StoreUpdateStateEvent(store, id, state));
		}

		public void Delete<TState>(Guid id)
		{
			var store = GetStoreByType(typeof(TState));
			_stateEvents.Add(new StoreDeleteStateEvent(store, id));
		}

		public void Delete(Type stateType, Guid id)
		{
			var store = GetStoreByType(stateType);
			_stateEvents.Add(new StoreDeleteStateEvent(store, id));
		}

		public void DeleteByRoot<TState>(Guid rootId)
		{
			var store = GetStoreByType(typeof(TState));
			_stateEvents.Add(new StoreDeleteRootStateEvent(store, rootId));
		}

		public void DeleteByRoot(Type stateType, Guid rootId)
		{
			var store = GetStoreByType(stateType);
			_stateEvents.Add(new StoreDeleteRootStateEvent(store, rootId));
		}

		public void DeleteWithSubentities<TState>(Guid rootId)
		{
			var stateType = typeof(TState);
			DeleteWithSubentities(stateType, rootId);
		}

		public void DeleteWithSubentities(Type stateType, Guid rootId)
		{
			var substores = GetStoresByRootType(stateType);
			foreach (var substore in substores)
			{
				_stateEvents.Add(new StoreDeleteRootStateEvent(substore, rootId));
			}

			var store = GetStoreByType(stateType);
			_stateEvents.Add(new StoreDeleteStateEvent(store, rootId));
		}

		public async Task Apply(Context context)
		{
			using var connection = new SqlConnection(_connectionString);
			connection.Open();
			using var transaction = await connection.BeginTransactionAsync();
			foreach (var ev in _stateEvents)
			{
				await ev.Apply(context, connection, transaction);
			}
			transaction.Commit();

			_stateEvents.Clear();
		}

		public async Task Apply(Guid rootId, IEnumerable<StateEvent> events, Context context)
		{
			await using var connection = new SqlConnection(_connectionString);
			await connection.OpenAsync();
			await using var transaction = await connection.BeginTransactionAsync();

			foreach (var ev in events)
			{
				var store = GetStoreByType(ev.State.GetType());

				switch (ev.Operation)
				{
					case StateEventOperation.Add:
						await store.Add(ev.EntityId, rootId, ev.ParentId, ev.State, context, connection, transaction);
						break;
					case StateEventOperation.Update:
						await store.Update(ev.EntityId, ev.State, context, connection, transaction);
						break;
					case StateEventOperation.Delete:
						await store.Delete(ev.EntityId, context, connection, transaction);
						break;
				}
			}
			
			await transaction.CommitAsync();
		}

		private MsSqlEntityStateStore GetStoreByType(Type stateType)
		{
			return _entityTypeStores.GetValueOrDefault(stateType.Name,
				new MsSqlEntityStateStore(_connectionString, stateType));
		}

		private IEnumerable<MsSqlEntityStateStore> GetStoresByRootType(Type stateType)
		{
			if (_subentityTypes.ContainsKey(stateType.Name))
			{
				var subentities = _subentityTypes[stateType.Name];
				return subentities.Select(GetStoreByType);
			}

			return Array.Empty<MsSqlEntityStateStore>();
		}

		private async Task LoadSubentityTypes()
		{
			const string query = $"SELECT [RootType], [EntityType] FROM [RootSubentityTypes]";

			await using var connection = new SqlConnection(_connectionString);
			var typeNames = await connection.QueryAsync(query);

			foreach(var item in typeNames)
			{
				var t = Type.GetType(item.EntityType);
				if (t == null)
				{
					throw new TypeLoadException($"Type {item.EntityType} not found");
				}

				if (!_subentityTypes.ContainsKey(item.RootType))
				{
					_subentityTypes[item.RootType] = new List<Type>();
				}

				_subentityTypes[item.RootType].Add(t);
			}
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			return LoadSubentityTypes();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
