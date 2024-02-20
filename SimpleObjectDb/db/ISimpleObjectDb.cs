namespace SimpleFileDatabase;

public interface ISimpleObjectDb
{
    public Task CreateAsync<Tdata>(Tdata data) where Tdata : class;
    public Task UpdateAsync<Tdata>(Tdata data) where Tdata : class;
    public Task<Tdata?> GetByIdAsync<Tdata>(object id) where Tdata : class;
    public IAsyncEnumerable<Tdata> GetAllAsync<Tdata>() where Tdata : class;
    public Task DeleteByIdAsync<Tdata>(object id) where Tdata : class;
}
