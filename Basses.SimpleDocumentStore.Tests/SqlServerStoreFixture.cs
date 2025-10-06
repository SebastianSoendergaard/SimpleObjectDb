using Basses.SimpleDocumentStore.SqlServer;
using Testcontainers.MsSql;

namespace Basses.SimpleDocumentStore.Tests;

public class SqlServerStoreFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
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
        return new SqlServerDocumentStore(_container.GetConnectionString(), config);
    }
}
