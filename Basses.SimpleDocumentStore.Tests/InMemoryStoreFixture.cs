using Basses.SimpleDocumentStore.InMemory;

namespace Basses.SimpleDocumentStore.Tests;

public class InMemoryStoreFixture
{
    public IDocumentStore CreateDocumentStore(DocumentStoreConfiguration config)
    {
        return new InMemoryDocumentStore(config);
    }
}
