namespace Basses.SimpleDocumentStore;

public interface IDocumentStore
{
    public Task CreateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class;
    public Task UpdateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class;
    public Task<Tdata?> GetByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class;
    public IAsyncEnumerable<Tdata> GetAllAsync<Tdata>(CancellationToken cancellationToken = default) where Tdata : class;
    public Task DeleteByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class;
    public Task DeleteAllAsync<Tdata>(CancellationToken cancellationToken = default) where Tdata : class;
}
