using System.Runtime.CompilerServices;

namespace Basses.SimpleDocumentStore.Files;

public class FileDocumentStore : IDocumentStore
{
    private readonly string _directoryPath;
    private readonly DocumentStoreConfiguration _configuration;

    public FileDocumentStore(string directoryPath, DocumentStoreConfiguration configuration)
    {
        _directoryPath = directoryPath;
        _configuration = configuration;

        Directory.CreateDirectory(_directoryPath);
    }

    public async Task CreateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var path = CreateFilePath<Tdata>(id);
        if (File.Exists(path))
        {
            throw new AlreadyExistException($"Id for {typeof(Tdata).FullName} already exist");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new ArgumentNullException("Path cannot be null"));

        var json = _configuration.Serializer.ToJson(data);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public async Task UpdateAsync<Tdata>(Tdata data, CancellationToken cancellationToken = default) where Tdata : class
    {
        var id = _configuration.GetIdFromData(data);
        var path = CreateFilePath<Tdata>(id);
        if (!File.Exists(path))
        {
            throw new NotFoundException($"Id for {typeof(Tdata).FullName} not found");
        }

        var json = _configuration.Serializer.ToJson(data);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public async Task<Tdata?> GetByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var path = CreateFilePath<Tdata>(id);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var data = _configuration.Serializer.FromJson<Tdata>(json);
        return data;
    }

    public async IAsyncEnumerable<Tdata> GetAllAsync<Tdata>([EnumeratorCancellation] CancellationToken cancellationToken = default) where Tdata : class
    {
        var path = CreateDirectoryPath<Tdata>();
        var files = Directory.EnumerateFiles(path);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var data = _configuration.Serializer.FromJson<Tdata>(json);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    public Task DeleteByIdAsync<Tdata>(object id, CancellationToken cancellationToken = default) where Tdata : class
    {
        var path = CreateFilePath<Tdata>(id);
        File.Delete(path);
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync<Tdata>(CancellationToken cancellationToken = default) where Tdata : class
    {
        var path = CreateDirectoryPath<Tdata>();
        if (!Directory.Exists(path))
        {
            return Task.CompletedTask;
        }

        var files = Directory.EnumerateFiles(path);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(file);
        }

        return Task.CompletedTask;
    }

    private string CreateFilePath<Tdata>(object id) where Tdata : class
    {
        if (id == null)
        {
            throw new ArgumentNullException($"Id for {typeof(Tdata).FullName} cannot be null");
        }

        var idString = id.ToString();
        if (string.IsNullOrWhiteSpace(idString))
        {
            throw new ArgumentNullException($"Id for {typeof(Tdata).FullName} cannot be empty");
        }

        return Path.Combine(_directoryPath, typeof(Tdata).Name, idString + ".json");
    }

    private string CreateDirectoryPath<Tdata>() where Tdata : class
    {
        return Path.Combine(_directoryPath, typeof(Tdata).Name);
    }
}
