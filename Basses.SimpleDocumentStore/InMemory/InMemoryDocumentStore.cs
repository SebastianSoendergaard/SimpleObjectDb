using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Basses.SimpleDocumentStore.InMemory;

public class InMemoryDocumentStore : IDocumentStore
{
    private readonly IDictionary<Type, IDictionary<object, string>> _store = new Dictionary<Type, IDictionary<object, string>>();
    private readonly DocumentStoreConfiguration _configuration;

    public InMemoryDocumentStore(DocumentStoreConfiguration configuration)
    {
        foreach (var type in configuration.IdConverters.Keys)
        {
            _store.Add(type, new Dictionary<object, string>());
        }

        _configuration = configuration;
    }

    public Task CreateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);

        var collection = GetDocumentCollection<Tdata>();
        if (collection.ContainsKey(id))
        {
            throw new AlreadyExistException($"Id for {typeof(Tdata).FullName} already exist");
        }

        var json = JsonSerializer.Serialize(data);

        collection.Add(id, json);
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync<Tdata>(CancellationToken cancellationToken = default) where Tdata : class
    {
        var collection = GetDocumentCollection<Tdata>();
        collection.Clear();
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var collection = GetDocumentCollection<Tdata>();
        collection.Remove(id);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<Tdata> GetAllAsync<Tdata>([EnumeratorCancellation] CancellationToken cancellationToken = default) where Tdata : class
    {
        var collection = GetDocumentCollection<Tdata>();
        foreach (var json in collection.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = JsonSerializer.Deserialize<Tdata>(json);
            if (data != null)
            {
                yield return await Task.FromResult(data);
            }
        }
    }

    public Task<Tdata?> GetByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var collection = GetDocumentCollection<Tdata>();
        if (collection.TryGetValue(id, out var json))
        {
            var data = JsonSerializer.Deserialize<Tdata>(json);
            return Task.FromResult(data);
        }
        return Task.FromResult<Tdata?>(null);
    }

    public Task UpdateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);

        var collection = GetDocumentCollection<Tdata>();
        if (!collection.ContainsKey(id))
        {
            throw new NotFoundException($"Id for {typeof(Tdata).FullName} not found");
        }

        var json = JsonSerializer.Serialize(data);

        collection[id] = json;
        return Task.CompletedTask;
    }

    private IDictionary<object, string> GetDocumentCollection<Tdata>() where Tdata : class
    {
        var type = typeof(Tdata);
        if (!_store.TryGetValue(type, out var collection))
        {
            throw new TypeNotRegisteredException($"{type.FullName} not registered");
        }
        return collection;
    }
}
