# Base application design elements

Packages with base application design elements:

 - Domain entities
 - StateStore
 - Repository

**Domain entities** -- interfaces and base classes implementation for use in domain model.

Entities **StateStore** interface and *InMemory*, *MsSql* and *MongoDB* implementations. Use StateStore to persist entity states in application services implemented using the transaction script pattern.

Generic **Repository** interface and implementation of repository pattern. Used in application services based on the domain model.

```C#
public interface IRepository<T> where T : class, IAggregateRootBase
{
    Task Add(T root, Context context);
    Task Update(T root, Context context);
    Task Delete(T root, Context context);

    Task<T?> Get(Guid id);
}
```
