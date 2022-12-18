using System.Data.SqlClient;
using System.Reflection;
using Dapper;
using Ccode.Adapters.Repository;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.Repository.MsSql
{
	public class MsSqlRepository<T> : IRepository<T> where T : class, IAggregateRootBase
	{
		private readonly ConstructorInfo _constructor;
		private readonly string _connectionString;
		private readonly MsSqlStateStore _rootStateStore;
		private readonly Type _rootType;
		private readonly Type _rootStateType;
		private readonly SortedList<string, MsSqlStateStore> _entityTypeStores = new SortedList<string, MsSqlStateStore>();
		private readonly List<MsSqlStateStore> _subentityStores = new List<MsSqlStateStore>();
		private bool _haveSubentities = true;
		private bool _initialized = false;

		public MsSqlRepository(string connectionString) 
		{
			_connectionString = connectionString;
			_rootType = typeof(T);
			var stateType = _rootType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))?.GetGenericArguments()[0];

			if (stateType == null)
			{
				throw new ArgumentException("Aggregate Root must implement IEntity interface");
			}

			_rootStateType = stateType;
			_constructor = GetConstructor();

			_rootStateStore = new MsSqlStateStore(connectionString, _rootStateType);
			_entityTypeStores.Add(_rootStateType.Name, _rootStateStore);
		}

		public async Task Add(T root, Context context)
		{			
			await _rootStateStore.Add(root.Id, root.StateObject);
		}

		public Task Delete(T root, Context context)
		{
			var tasks = new List<Task>(_subentityStores.Count + 1);
			foreach(var store in _subentityStores)
			{
				tasks.Add(store.DeleteByRoot(root.Id));
			}

			tasks.Add(_rootStateStore.Delete(root.Id));

			return Task.WhenAll(tasks);
		}

		public async Task Update(T root, Context context)
		{
			var events = root.GetStateEvents();

			foreach(var ev in events)
			{
				var stateStore = _entityTypeStores[ev.State.GetType().Name];

				switch(ev.Operation)
				{
					case StateEventOperation.Add: 
						await stateStore.Add(ev.EntityId, root.Id, ev.ParentId, ev.State);
						break;
					case StateEventOperation.Update:
						await stateStore.Update(ev.EntityId, ev.State);
						break;
					case StateEventOperation.Delete:
						await stateStore.Delete(ev.EntityId);
						break;
				}
			}
		}

		public async Task<T?> Get(Guid id)
		{
			if (_haveSubentities && !_initialized)
			{
				var subentityTypeNames = await LoadSubentityTypes();
				InitSubentityStateStores(subentityTypeNames);
				_initialized = true;
			}

			var state = await _rootStateStore.Get(id);

			if (state == null)
			{
				return null;
			}

			object? instance;
			if (_haveSubentities)
			{
				var substates = new List<EntityData>();
				foreach(var store in _subentityStores)
				{
					substates.AddRange(await store.GetByRoot(id));
				}

				instance = _constructor?.Invoke(new object[] { id, state, substates.ToArray() });
			}
			else
			{
				instance = _constructor?.Invoke(new object[] { id, state });
			}

			return (T?)instance;
		}

		private void InitSubentityStateStores(string[] subentityTypeNames)
		{
			foreach (var subentityTypeName in subentityTypeNames)
			{
				var type = Type.GetType(subentityTypeName);

				if (type == null)
				{
					throw new NullReferenceException();
				}

				var stateStore = new MsSqlStateStore(_connectionString, type);
				_entityTypeStores.Add(type.Name, stateStore);
				_subentityStores.Add(stateStore);
			}
		}

		private ConstructorInfo GetConstructor()
		{
			// constructor with first Guid parameter implies entity id 
			//ConstructorInfo? ctor = _rootType.GetConstructors().First(c => c.GetParameters().First().ParameterType == typeof(Guid));

			var ctorParamTypes = new[] { typeof(Guid), _rootStateType, typeof(EntityData[]) };
			ConstructorInfo? ctor = _rootType.GetConstructor(ctorParamTypes);

			if (ctor == null)
			{
				ctor = _rootType.GetConstructor(new[] { typeof(Guid), _rootStateType });
				_haveSubentities = false;
			}

			if (ctor == null)
			{
				throw new ArgumentException("Need a constructor with the first parameter Guid");
			}

			return ctor;
		}

		private async Task<string[]> LoadSubentityTypes()
		{
			var query = $"SELECT [EntityType] FROM [RootSubentityTypes] WHERE [RootType] = @rootTypeName";

			using var connection = new SqlConnection(_connectionString);
			var rootTypeName = _rootStateType.Name;
			var typeNames = await connection.QueryAsync<string>(query, new { rootTypeName });

			return typeNames.ToArray();
		}
	}

	public class EntityData
	{
		public EntityData(Guid id, Guid rootId, Guid? parentId, object state)
		{
			RootId = rootId;
			ParentId = parentId;
			Id = id;
			State = state;
		}

		public Guid Id { get; }
		public Guid RootId { get; }
		public Guid? ParentId { get; }
		public object State { get; }
	}
}
