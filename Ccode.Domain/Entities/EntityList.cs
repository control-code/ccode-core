using System.Collections;

namespace Ccode.Domain.Entities
{
	public class EntityList<T, TState> : IList<T> where T : IEntity<TState>
	{
		private readonly List<T> _entities = new();
		private readonly Tracker _tracker;
		private readonly EntityBase _owner;

		public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public int Count => throw new NotImplementedException();

		public bool IsReadOnly => throw new NotImplementedException();

		public EntityList(EntityBase owner)
		{
			_owner = owner;
			_tracker = owner.Tracker;
		}

		public EntityList(EntityBase owner, IEnumerable<T> entities)
		{
			_owner = owner;
			_tracker = owner.Tracker;
			_entities.AddRange(entities);
		}

		public EntityList(EntityBase owner, EntityFactory<T, TState> factory, StateInfo[] subentities)
		{
			_owner = owner;
			_tracker = owner.Tracker;

			var entities = subentities.Where(i => i.ParentId == owner.Id).Select(i => factory.Create(i.Id, (TState)i.State, subentities));

			_entities.AddRange(entities);
		}

		public void Add(T entity)
		{
			_entities.Add(entity);
			_tracker.AddStateEvent(new StateEvent(entity.Id, _owner.Id, StateEventOperation.Add, entity.StateObject));
		}

		public void Clear()
		{
			foreach(var entity in _entities)
			{
				_tracker.AddStateEvent(new StateEvent(entity.Id, _owner.Id, StateEventOperation.Delete, entity.StateObject));
			}

			_entities.Clear();
		}

		public bool Contains(T entity)
		{
			return _entities.Contains(entity);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_entities.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _entities.GetEnumerator();
		}

		public int IndexOf(T entity)
		{
			return _entities.IndexOf(entity);
		}

		public void Insert(int index, T entity)
		{
			_entities.Insert(index, entity);
			_tracker.AddStateEvent(new StateEvent(entity.Id, _owner.Id, StateEventOperation.Add, entity.StateObject));
		}

		public bool Remove(T entity)
		{
			var result = _entities.Remove(entity);
			if (result)
			{
				_tracker.AddStateEvent(new StateEvent(entity.Id, _owner.Id, StateEventOperation.Delete, entity.StateObject));
			}
			return result;
		}

		public void RemoveAt(int index)
		{
			var entity = _entities[index];
			_entities.RemoveAt(index);
			_tracker.AddStateEvent(new StateEvent(entity.Id, _owner.Id, StateEventOperation.Delete, entity.StateObject));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _entities.GetEnumerator();
		}
	}
}
