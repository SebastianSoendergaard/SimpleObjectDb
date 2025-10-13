using Basses.SimpleDocumentStore;

namespace SimpleObjectDbWebApi.ModuleA;

public class Repository(IDocumentStore store)
{
    public Task CreateAsync(A a, CancellationToken cancellationToken = default)
    {
        return store.CreateAsync(a, cancellationToken);
    }
    public Task UpdateAsync(A a, CancellationToken cancellationToken = default)
    {
        return store.UpdateAsync(a, cancellationToken);
    }
    public Task<A?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return store.GetByIdAsync<A>(id, cancellationToken);
    }
}
