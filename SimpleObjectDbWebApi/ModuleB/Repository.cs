using Basses.SimpleDocumentStore;

namespace SimpleObjectDbWebApi.ModuleB;

public class Repository(IDocumentStore store)
{
    public Task CreateAsync(B b, CancellationToken cancellationToken = default)
    {
        return store.CreateAsync(b, cancellationToken);
    }
    public Task UpdateAsync(B b, CancellationToken cancellationToken = default)
    {
        return store.UpdateAsync(b, cancellationToken);
    }
    public Task<B?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return store.GetByIdAsync<B>(id, cancellationToken);
    }
}
