using Basses.SimpleDocumentStore.InMemory;

namespace Basses.SimpleDocumentStore.Tests.Fixtures;

public class InMemoryStoreFixture
{
    public IDocumentStore CreateDocumentStore(DocumentStoreConfiguration config)
    {
        return new InMemoryDocumentStore(config);
    }
}
