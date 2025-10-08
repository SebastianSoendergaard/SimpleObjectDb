using Basses.SimpleDocumentStore.Files;

namespace Basses.SimpleDocumentStore.Tests.Fixtures;

public class FileStoreFixture : IDisposable
{
    private readonly string _directoryPrefix = @"c:/temp/documentstore_test_";
    private readonly List<string> _names = [];

    public IDocumentStore CreateDocumentStore(DocumentStoreConfiguration config)
    {
        var name = Guid.NewGuid().ToString()[..8];
        _names.Add(name);
        return new FileDocumentStore(_directoryPrefix + name, config);
    }

    public void Dispose()
    {
        foreach (var name in _names)
        {
            Directory.Delete(_directoryPrefix + name, true);
        }
        _names.Clear();
    }
}
