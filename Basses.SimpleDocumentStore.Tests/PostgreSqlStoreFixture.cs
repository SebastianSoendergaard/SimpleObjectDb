using Basses.SimpleDocumentStore.PostgreSql;
using Testcontainers.PostgreSql;

namespace Basses.SimpleDocumentStore.Tests;

public class PostgreSqlStoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public IDocumentStore CreateDocumentStore(DocumentStoreConfiguration config)
    {
        return new PostgreSqlDocumentStore(_container.GetConnectionString(), config);
    }
}
