using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Dapper;
using Ccode.Domain;
using Ccode.Domain.Entities;

namespace Ccode.AdaptersImpl.StateStore.MsSql
{
	internal class MsSqlEntityStateStore
	{
		private readonly Type _stateType;
		private readonly string _connectionStr;
		private readonly string _tableName;
		private readonly string _selectColumnList;
		private readonly string _insertColumnList;
		private readonly string _insertValueList;
		private readonly string _updateList;

		public string GetByIdQuery { get; }
		public string GetByRootIdQuery { get; }

		public MsSqlEntityStateStore(string connectionStr, Type stateType)
		{
			_connectionStr = connectionStr;

			_stateType = stateType;

			_tableName = GetTableName(_stateType);
			_selectColumnList = GetSelectColumnList(_stateType);
			_insertColumnList = GetInsertColumnList(_stateType);
			_insertValueList = GetInsertValueList(_stateType);
			_updateList = GetUpdateList(_stateType);

			GetByIdQuery = $"SELECT TOP 1 {_selectColumnList} FROM {_tableName} WHERE [Id] = @id";
			GetByRootIdQuery = $"SELECT [Id], [RootId], [ParentId], {_selectColumnList} FROM {_tableName} WHERE [RootId] = @rootId";
		}

		public async Task<object?> Get(Guid id)
		{
			using var connection = new SqlConnection(_connectionStr);
			var state = await connection.QuerySingleOrDefaultAsync(_stateType, GetByIdQuery, new { id });

			return state;
		}

		public async Task<object?> Get(DbDataReader reader)
		{
			if (await reader.ReadAsync())
			{
				return CreateState(reader);
			}

			return null;
		}

		public Task<StateInfo[]> GetByRoot(Guid rootId)
		{
			using var connection = new SqlConnection(_connectionStr);

			return GetByRoot(rootId, connection, null);
		}

		public async Task<StateInfo[]> GetByRoot(Guid rootId, SqlConnection connection, IDbTransaction? transaction)
		{
			var reader = await connection.ExecuteReaderAsync(GetByRootIdQuery, new { rootId }, transaction);

			var list = new List<StateInfo>();
			while (await reader.ReadAsync())
			{
				list.Add(new StateInfo(reader.GetGuid(0), reader.GetGuid(1), reader.IsDBNull(2) ? null : reader.GetGuid(2), CreateState(reader)));
			}

			return list.ToArray();
		}

		public async Task<StateInfo[]> GetByRoot(DbDataReader reader)
		{
			var list = new List<StateInfo>();
			while (await reader.ReadAsync())
			{
				list.Add(new StateInfo(reader.GetGuid(0), reader.GetGuid(1), reader.IsDBNull(2) ? null : reader.GetGuid(2), CreateState(reader)));
			}

			return list.ToArray();
		}

		public Task Add(Guid id, object state, Context context, SqlConnection connection, IDbTransaction transaction)
		{
			return Add(id, id, null, state, context, connection, transaction);
		}

		public Task Add(Guid id, Guid rootId, object state, Context context, SqlConnection connection, IDbTransaction transaction)
		{
			return Add(id, rootId, rootId, state, context, connection, transaction);
		}

		public async Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context, SqlConnection connection, IDbTransaction? transaction = null)
		{
			var cmd = $"INSERT INTO {_tableName} ({_insertColumnList}) VALUES ({_insertValueList})";

			var parameters = new DynamicParameters(state);
			parameters.Add("Id", id);
			parameters.Add("RootId", rootId);
			parameters.Add("ParentId", parentId);

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, parameters, transaction);
		}

		public async Task Update(Guid id, object state, Context context, SqlConnection connection, IDbTransaction? transaction = null)
		{
			var cmd = $"UPDATE {_tableName} SET {_updateList} WHERE [Id] = @id";

			var parameters = new DynamicParameters(state);
			parameters.Add("Id", id);

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, parameters, transaction);
		}

		public async Task Delete(Guid id, Context context, SqlConnection connection, IDbTransaction? transaction = null)
		{
			var cmd = $"DELETE FROM {_tableName} WHERE [Id] = @id";

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, new { id }, transaction);
		}

		public async Task DeleteByRoot(Guid rootId, Context context, SqlConnection connection, IDbTransaction? transaction = null)
		{
			var cmd = $"DELETE FROM {_tableName} WHERE [RootId] = @rootId";

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, new { rootId }, transaction);
		}

		private object CreateState(DbDataReader reader)
		{
			var ctor = _stateType.GetConstructors()[0];
			var parameters = ctor.GetParameters();
			var parr = parameters.Select(p => reader[p.Name]).ToArray();
			var state = ctor.Invoke(parr);
			return state;
		}

		private string GetSelectColumnList(Type stateType)
		{
			var list = new StringBuilder();
			var properties = stateType.GetProperties();
			foreach (var p in properties)
			{
				if (list.Length > 0)
				{
					list.Append(", ");
				}
				list.Append("[");
				list.Append(p.Name);
				list.Append("]");
			}

			return list.ToString();
		}

		private string GetInsertValueList(Type stateType)
		{
			var list = new StringBuilder("@id, @rootId, @parentId");
			var properties = stateType.GetProperties();
			foreach (var p in properties)
			{
				list.Append(", @");
				list.Append(p.Name);
			}

			return list.ToString();
		}

		private string GetInsertColumnList(Type stateType)
		{
			var list = new StringBuilder("[Id], [RootId], [ParentId]");
			var properties = stateType.GetProperties();
			foreach (var p in properties)
			{
				list.Append(", [");
				list.Append(p.Name);
				list.Append("]");
			}

			return list.ToString();
		}

		private string GetUpdateList(Type stateType)
		{
			var list = new StringBuilder();
			var properties = stateType.GetProperties();
			foreach (var p in properties)
			{
				if (list.Length > 0)
				{
					list.Append(", ");
				}
				list.Append("[");
				list.Append(p.Name);
				list.Append("] = @");
				list.Append(p.Name);
			}

			return list.ToString();
		}

		private string GetTableName(Type stateType)
		{
			return stateType.Name + "s";
		}
	}
}
