namespace Ccode.AdaptersImpl.StateStore.Mongo
{
	public class TypeNotFoundException : Exception
	{
		public TypeNotFoundException(string typeName): base($"Type name {typeName} not found") 
		{ }
	}
}