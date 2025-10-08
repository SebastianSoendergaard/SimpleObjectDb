using AutoFixture;
using Basses.SimpleDocumentStore.SqlServer;
using Basses.SimpleDocumentStore.Tests.Fixtures;
using static Basses.SimpleDocumentStore.Tests.DocumentStoreTests;

namespace Basses.SimpleDocumentStore.Tests;

public class SqlServerDocumentStoreTests : IClassFixture<SqlServerStoreFixture>
{
    private Fixture _fixture = new();
    private static SqlServerStoreFixture? _storeFixture;

    public SqlServerDocumentStoreTests(SqlServerStoreFixture storeFixture)
    {
        _storeFixture = storeFixture;
    }

    [Fact]
    public async Task UsesDefaultSchemaWhenNoSpecificConfigured()
    {
        var config = new DocumentStoreConfiguration();
        config.RegisterDataType<TestObjectA>(x => x.Id);
        var store = _storeFixture!.CreateDocumentStore(config);

        var obj = _fixture.Create<TestObjectA>();

        await store.CreateAsync(obj);
        var storedObj = await store.GetByIdAsync<TestObjectA>(obj.Id);

        var sqlserverStore = (SqlServerDocumentStore)store;
        var sqlHelper = new SqlServerHelper(sqlserverStore.ConnectionString);
        var sql = $"SELECT data FROM dbo.TestObjectA WHERE id = '{obj.Id}'";
        var postgresContent = await sqlHelper.QuerySingleOrDefaultAsync(sql, [], reader => reader.GetString(0));

        Assert.Equal(obj?.AsString(), storedObj.AsString());
        Assert.Equal(obj?.AsString(), postgresContent?.Replace(" ", ""));
    }

    [Fact]
    public async Task UsesConfiguredSchema()
    {
        var schema = RandomString(10);

        var config = new DocumentStoreConfiguration();
        config.RegisterDataType<TestObjectA>(x => x.Id, schema);
        var store = _storeFixture!.CreateDocumentStore(config);

        var obj = _fixture.Create<TestObjectA>();

        await store.CreateAsync(obj);
        var storedObj = await store.GetByIdAsync<TestObjectA>(obj.Id);

        var sqlserverStore = (SqlServerDocumentStore)store;
        var sqlHelper = new SqlServerHelper(sqlserverStore.ConnectionString);
        var sql = $"SELECT data FROM {schema}.TestObjectA WHERE id = '{obj.Id}'";
        var postgresContent = await sqlHelper.QuerySingleOrDefaultAsync(sql, [], reader => reader.GetString(0));

        Assert.Equal(obj?.AsString(), storedObj.AsString());
        Assert.Equal(obj?.AsString(), postgresContent?.Replace(" ", ""));
    }

    private static Random random = new Random();
    public static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
