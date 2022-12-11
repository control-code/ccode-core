using System.Reflection;
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
			ExtractEntitiyTypesFromConstructorParameters();

			_rootStateStore = new MsSqlStateStore(connectionString, _rootStateType);
		}

		public async Task Add(T root, Context context)
		{			
			await _rootStateStore.Add(root.Id, root.StateObject);
		}

		public async Task Delete(T root, Context context)
		{
			await _rootStateStore.Delete(root.Id);
		}

		public async Task Update(T root, Context context)
		{
			var events = root.GetStateEvents();

			foreach(var ev in events)
			{
				switch(ev.Operation)
				{
					case StateEventOperation.Add: 
						await _rootStateStore.Add(ev.EntityId, ev.State);
						break;
					case StateEventOperation.Update:
						await _rootStateStore.Update(ev.EntityId, ev.State);
						break;
					case StateEventOperation.Delete:
						await _rootStateStore.Delete(ev.EntityId);
						break;
				}
			}
		}

		public async Task<T?> Get(Guid id)
		{
			var state = await _rootStateStore.Get(id);

			if (state == null)
			{
				return null;
			}

			ConstructorInfo? ctor = typeof(T).GetConstructor(new[] { typeof(Guid), _rootStateType });
			object? instance = ctor?.Invoke(new object[] { id, state });

			return (T?)instance;
		}

		private ConstructorInfo GetConstructor()
		{
			// constructor with first Guid parameter implies entity id 
			ConstructorInfo? ctor = _rootType.GetConstructors().First(c => c.GetParameters().First().ParameterType == typeof(Guid));

			if (ctor == null)
			{
				throw new ArgumentException("Need a constructor with the first parameter Guid");
			}

			return ctor;
		}

		private void ExtractEntitiyTypesFromConstructorParameters()
		{
			var subrepositories = new SortedList<Guid, IStateStore>();
			var parameters = _constructor.GetParameters().Skip(1);
			foreach(var p in parameters)
			{
				var et = p.ParameterType.GetElementType();
				if (p.ParameterType.IsArray && et!=null && et.IsAssignableTo(typeof(IEntity<>)))
				{
					var sr = new MsSqlStateStore(_connectionString, et.GetGenericArguments()[0]);
					subrepositories.Add(et.GUID, sr);
				}

				if (p.ParameterType.IsArray && et != null && et.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntity<>)))
				{

				}
			}
		}
	}

	public class EntityData
	{
		public EntityData(Guid rootId, Guid parentId, Guid id, object state)
		{
			RootId = rootId;
			ParentId = parentId;
			Id = id;
			State = state;
		}

		public Guid  RootId { get; }
		public Guid ParentId { get; }
		public Guid Id { get; }
		public object State { get; }
	}
}
