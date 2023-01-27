using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using Microsoft.Extensions.Hosting;
using Dapper;
using Ccode.Adapters.StateStore;
using Ccode.Domain;
using Ccode.Domain.Entities;

namespace Ccode.AdaptersImpl.StateStore.MsSql
{
	public class MsSqlStateStore : IStateStore, IHostedService
	{
		private readonly string _connectionString;
		private readonly ConcurrentDictionary<string, MsSqlEntityStateStore> _entityTypeStores = new ();
		private readonly List<MsSqlEntityStateStore> _subentityStores = new ();
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

		public Task<States?> GetByRoot<TState>(Guid rootId)
		{
			return GetByRoot(typeof(TState), rootId);
		}

		public async Task<States?> GetByRoot(Type stateType, Guid rootId)
		{
			var query = new StringBuilder();

			var store = GetStoreByType(stateType);
			query.Append(store.GetByIdQuery);
			query.AppendLine(";");

			var substores = GetStoresByRootType(stateType);
			foreach (var substore in substores)
			{
				query.Append(substore.GetByRootIdQuery);
				query.AppendLine(";");
			}

			await using var connection = new SqlConnection(_connectionString);
			var reader = await connection.ExecuteReaderAsync(query.ToString(), new { id = rootId, rootId });

			var rootState = await store.Get(reader);

			if (rootState == null)
			{
				return null;
			}

			var substates = new List<StateInfo>();
			foreach (var substore in substores)
			{
				reader.NextResult();
				substates.Union(await substore.GetByRoot(reader));
			}

			return new States(rootState, substates.ToArray());
		}

		public Task Add(Guid id, Guid rootId, object state, Context context)
		{
			return Add(id, rootId, null, state, context);
		}

		public async Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context)
		{
			if (id == rootId)
			{
				throw new ArgumentException("Substate id must not be equal root id");
			}

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

		public async Task AddRoot(Guid id, object state, Context context)
		{
			var store = GetStoreByType(state.GetType());
			await using var connection = new SqlConnection(_connectionString);
			await store.Add(id, id, null, state, context, connection);
		}

		public Task AddRoot(Guid id, object state, IEnumerable<StateEvent> events, Context context)
		{
			return Apply(id, events.Prepend(new StateEvent(id, null, StateEventOperation.Add, state)), context);
		}

		public Task DeleteRoot<TState>(Guid rootId, Context context)
		{
			return DeleteRoot(typeof(TState), rootId, context);
		}

		public async Task DeleteRoot(Type stateType, Guid rootId, Context context)
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
