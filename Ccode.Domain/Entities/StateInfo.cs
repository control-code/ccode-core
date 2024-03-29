﻿namespace Ccode.Domain.Entities
{
	public class StateInfo
	{
		public StateInfo(Guid id, Guid rootId, Guid? parentId, object state)
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
