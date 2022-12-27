using System.Data.SqlClient;
using System.Text;
using Dapper;
using Ccode.Domain;
using Ccode.Adapters.StateStore;
using System.Transactions;
using System.Data;

namespace Ccode.AdaptersImpl.StateStore.MsSql
{
	public class MsSqlEntityStateStore
	{
		private readonly Type _stateType;
		private readonly string _connectionStr;
		private readonly string _tableName;
		private readonly string _selectColumnList;
		private readonly string _insertColumnList;
		private readonly string _insertValueList;
		private readonly string _updateList;

		public MsSqlEntityStateStore(string connectionStr, Type stateType)
		{
			_connectionStr = connectionStr;

			_stateType = stateType;

			_tableName = GetTableName(_stateType);
			_selectColumnList = GetSelectColumnList(_stateType);
			_insertColumnList = GetInsertColumnList(_stateType);
			_insertValueList = GetInsertValueList(_stateType);
			_updateList = GetUpdateList(_stateType);
		}

		public async Task<object?> Get(Guid id)
		{
			var query = $"SELECT {_selectColumnList} FROM {_tableName} WHERE [Id] = @id";

			using var connection = new SqlConnection(_connectionStr);
			var state = await connection.QuerySingleOrDefaultAsync(_stateType, query, new { id });

			return state;
		}

		public async Task<EntityData[]> GetByRoot(Guid rootId)
		{
			var query = $"SELECT [Id], [RootId], [ParentId], {_selectColumnList} FROM {_tableName} WHERE [RootId] = @rootId";

			using var connection = new SqlConnection(_connectionStr);
			var reader = await connection.ExecuteReaderAsync(query, new { rootId });

			var list = new List<EntityData>();
			while (await reader.ReadAsync())
			{
				var ctor = _stateType.GetConstructors()[0];
				var parameters = ctor.GetParameters();
				var parr = parameters.Select(p => reader[p.Name]).ToArray();
				var state = ctor.Invoke(parr);
				list.Add(new EntityData(reader.GetGuid(0), reader.GetGuid(1), reader.IsDBNull(2) ? null : reader.GetGuid(2), state));
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

		public async Task Add(Guid id, Guid rootId, Guid? parentId, object state, Context context, SqlConnection connection, IDbTransaction transaction)
		{
			var cmd = $"INSERT INTO {_tableName} ({_insertColumnList}) VALUES ({_insertValueList})";

			var parameters = new DynamicParameters(state);
			parameters.Add("Id", id);
			parameters.Add("RootId", rootId);
			parameters.Add("ParentId", parentId);

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, parameters, transaction);
		}

		public async Task Update(Guid id, object state, Context context, SqlConnection connection, IDbTransaction transaction)
		{
			var cmd = $"UPDATE {_tableName} SET {_updateList} WHERE [Id] = @id";

			var parameters = new DynamicParameters(state);
			parameters.Add("Id", id);

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, parameters, transaction);
		}

		public async Task Delete(Guid id, Context context, SqlConnection connection, IDbTransaction transaction)
		{
			var cmd = $"DELETE FROM {_tableName} WHERE [Id] = @id";

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, new { id }, transaction);
		}

		public async Task DeleteByRoot(Guid rootId, Context context, SqlConnection connection, IDbTransaction transaction)
		{
			var cmd = $"DELETE FROM {_tableName} WHERE [RootId] = @rootId";

			//using var connection = new SqlConnection(_connectionStr);
			await connection.ExecuteAsync(cmd, new { rootId }, transaction);
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
