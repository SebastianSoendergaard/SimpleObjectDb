namespace Basses.SimpleDocumentStore;

public interface ISimpleObjectDb
{
    public Task CreateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class;
    public Task UpdateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class;
    public Task<Tdata?> GetByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class;
    public IAsyncEnumerable<Tdata> GetAllAsync<Tdata>(CancellationToken cancellationToken = default) where Tdata : class;
    public Task DeleteByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class;
}
