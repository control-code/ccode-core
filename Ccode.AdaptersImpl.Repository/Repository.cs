using System.Reflection;
using Ccode.Adapters.Repository;
using Ccode.Adapters.StateStore;
using Ccode.Domain;

namespace Ccode.AdaptersImpl.Repository
{
	public class Repository<T> : IRepository<T> where T : class, IAggregateRootBase
	{
		private readonly Type _rootType;
		private readonly Type _rootStateType;
		private readonly ConstructorInfo _constructor; 
		private readonly IStateStore _store;
		private readonly Type[] _subentityTypes;

		public Repository(IStateStore store, IEnumerable<Type> subentityTypes) 
		{
			_store = store;
			_subentityTypes = subentityTypes.ToArray();

			_rootType = typeof(T);
			var stateType = _rootType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))?.GetGenericArguments()[0];

			if (stateType == null)
			{
				throw new ArgumentException("Aggregate Root must implement IEntity interface");
			}

			_rootStateType = stateType;
			_constructor = GetConstructor();
		}

		public async Task<T?> Get(Guid id)
		{
			var state = await _store.Get(_rootStateType, id);

			if (state == null)
			{
				return null;
			}

			object? instance;
			if (_subentityTypes.Length > 0)
			{
				var substates = new List<EntityData>();
				foreach (var t in _subentityTypes)
				{
					substates.AddRange(await _store.GetByRoot(t, id));
				}

				instance = _constructor?.Invoke(new object[] { id, state, substates.Where(i => i.Id != id).ToArray() });
			}
			else
			{
				instance = _constructor?.Invoke(new object[] { id, state });
			}

			return (T?)instance;
		}

		public async Task Add(T root, Context context)
		{
			var events = root.GetStateEvents();
			//if (events.)
			var addEvent = new StateEvent(root.Id, null, StateEventOperation.Add, root.StateObject);
			//await _store.Add(root.Id, root.StateObject, context);
			await _store.Apply(root.Id, events.Prepend(addEvent), context);
		}

		public Task Update(T root, Context context)
		{
			var events = root.GetStateEvents();
			return _store.Apply(root.Id, events, context);
		}

		public Task Delete(T root, Context context)
		{
			return _store.DeleteWithSubstates(_rootStateType, root.Id, context);
		}

		private ConstructorInfo GetConstructor()
		{
			// constructor with first Guid parameter implies entity id 

			var ctorParamTypes = new[] { typeof(Guid), _rootStateType, typeof(EntityData[]) };
			ConstructorInfo? ctor = _rootType.GetConstructor(ctorParamTypes);

			if (ctor == null)
			{
				ctor = _rootType.GetConstructor(new[] { typeof(Guid), _rootStateType });
			}

			if (ctor == null)
			{
				throw new ArgumentException("Need a constructor with the first parameter Guid");
			}

			return ctor;
		}
	}
}
