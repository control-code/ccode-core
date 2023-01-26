using System.Reflection;

namespace Ccode.Domain.Entities
{
	public class EntityFactory<T, TState> where T : IEntity<TState>
	{
		private static readonly Type[] _ctorParamTypes = { typeof(Guid), typeof(TState) };
		private static readonly Type[] _ctorExtParamTypes = { typeof(Guid), typeof(TState), typeof(StateInfo[]) };

		private readonly ConstructorInfo _constructor;
		private readonly bool _extConstructor = true;

		public EntityFactory()
		{
			var entityType = typeof(T);

			ConstructorInfo? ctor = entityType.GetConstructor(_ctorExtParamTypes);

			if (ctor == null)
			{
				ctor = entityType.GetConstructor(_ctorParamTypes);
				_extConstructor = false;
			}

			if (ctor == null)
			{
				throw new ArgumentException("Need a constructor with the first parameter Guid");
			}

			_constructor = ctor;
		}

		public T Create(Guid id, TState state, StateInfo[] substates)
		{
			object instance;

			if (state == null)
			{
				throw new ArgumentNullException("State must not be null");
			}

			if (_extConstructor)
			{
				instance = _constructor.Invoke(new object[] { id, state, substates });
			}
			else
			{
				instance = _constructor.Invoke(new object[] { id, state });
			}

			return (T)instance;
		}
	}
}
