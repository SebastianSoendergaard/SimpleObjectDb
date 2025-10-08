using AutoFixture;
using Basses.SimpleDocumentStore.Files;
using Basses.SimpleDocumentStore.Tests.Fixtures;
using static Basses.SimpleDocumentStore.Tests.DocumentStoreTests;

namespace Basses.SimpleDocumentStore.Tests;

public class FileDocumentStoreTests : IClassFixture<FileStoreFixture>
{
    private Fixture _fixture = new();
    private static FileStoreFixture? _storeFixture;

    public FileDocumentStoreTests(FileStoreFixture storeFixture)
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

        var fileStore = (FileDocumentStore)store;
        var expectedFilePath = Path.Combine(fileStore.DirectoryPath, nameof(TestObjectA), obj.Id.ToString() + ".json");
        var fileContent = File.ReadAllText(expectedFilePath);

        Assert.Equal(obj?.AsString(), storedObj.AsString());
        Assert.Equal(obj?.AsString(), fileContent);
    }

    [Fact]
    public async Task UsesConfiguredSchema()
    {
        var schema = Guid.NewGuid().ToString().Replace("-", "");

        var config = new DocumentStoreConfiguration();
        config.RegisterDataType<TestObjectA>(x => x.Id, schema);
        var store = _storeFixture!.CreateDocumentStore(config);

        var obj = _fixture.Create<TestObjectA>();

        await store.CreateAsync(obj);
        var storedObj = await store.GetByIdAsync<TestObjectA>(obj.Id);

        var fileStore = (FileDocumentStore)store;
        var expectedFilePath = Path.Combine(fileStore.DirectoryPath, schema, nameof(TestObjectA), obj.Id.ToString() + ".json");
        var fileContent = File.ReadAllText(expectedFilePath);

        Assert.Equal(obj?.AsString(), storedObj.AsString());
        Assert.Equal(obj?.AsString(), fileContent);
    }
}
