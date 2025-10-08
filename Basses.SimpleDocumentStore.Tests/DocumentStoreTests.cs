using AutoFixture;
using Basses.SimpleDocumentStore.Tests.Fixtures;

namespace Basses.SimpleDocumentStore.Tests;

public sealed class DocumentStoreTests :
    IClassFixture<FileStoreFixture>,
    IClassFixture<PostgreSqlStoreFixture>,
    IClassFixture<SqlServerStoreFixture>,
    IClassFixture<InMemoryStoreFixture>
{
    private Fixture _fixture = new();
    private static InMemoryStoreFixture? _inMemoryStoreFixture;
    private static FileStoreFixture? _fileStoreFixture;
    private static PostgreSqlStoreFixture? _postgreSqlStoreFixture;
    private static SqlServerStoreFixture? _sqlServerStoreFixture;

    private DocumentStoreConfiguration _config = new DocumentStoreConfiguration();

    public DocumentStoreTests(
        InMemoryStoreFixture inMemoryStoreFixture,
        FileStoreFixture fileStoreFixture,
        PostgreSqlStoreFixture postgreSqlStoreFixture,
        SqlServerStoreFixture sqlServerStoreFixture)
    {
        _inMemoryStoreFixture = inMemoryStoreFixture;
        _fileStoreFixture = fileStoreFixture;
        _postgreSqlStoreFixture = postgreSqlStoreFixture;
        _sqlServerStoreFixture = sqlServerStoreFixture;

        _config.RegisterDataType<TestObjectA>(x => x.Id);
        _config.RegisterDataType<TestObjectB>(x => x.Id);
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task ReturnsNullWhenNoDocument(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var id = _fixture.Create<int>();
        var storedObj = await store.GetByIdAsync<TestObjectA>(id);

        Assert.Null(storedObj);
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task CanCreateAndGetADocument(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var obj = _fixture.Create<TestObjectA>();

        await store.CreateAsync(obj);
        var storedObj = await store.GetByIdAsync<TestObjectA>(obj.Id);

        Assert.NotNull(storedObj);
        Assert.Equal(obj.AsString(), storedObj.AsString());
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task CanUpdateAnExistingDocument(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var originalObj = _fixture.Create<TestObjectA>();
        var updatedObj = _fixture.Create<TestObjectA>() with { Id = originalObj.Id };

        await store.CreateAsync(originalObj);
        await store.UpdateAsync(updatedObj);
        var storedObj = await store.GetByIdAsync<TestObjectA>(originalObj.Id);

        Assert.NotNull(storedObj);
        Assert.Equal(updatedObj.AsString(), storedObj.AsString());
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task CanDeleteADocument(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var obj = _fixture.Create<TestObjectA>();

        await store.CreateAsync(obj);
        await store.DeleteByIdAsync<TestObjectA>(obj.Id);
        var storedObj = await store.GetByIdAsync<TestObjectA>(obj.Id);

        Assert.Null(storedObj);
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task CanStoreDifferentTypesWithSameId(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();
        await store.DeleteAllAsync<TestObjectB>();

        var objA = _fixture.Create<TestObjectA>();
        var objB = _fixture.Create<TestObjectB>();

        await store.CreateAsync(objA);
        await store.CreateAsync(objB);
        var storedObjA = await store.GetByIdAsync<TestObjectA>(objA.Id);
        var storedObjB = await store.GetByIdAsync<TestObjectB>(objB.Id);

        Assert.NotNull(storedObjA);
        Assert.NotNull(storedObjB);
        Assert.Equal(objA.AsString(), storedObjA.AsString());
        Assert.Equal(objB.AsString(), storedObjB.AsString());
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task CanGetAllDocumentsOfATypeAtOnce(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var collection = _fixture.CreateMany<TestObjectA>();

        foreach (var obj in collection)
        {
            await store.CreateAsync(obj);
        }

        var storedCollection = new List<TestObjectA>();
        var enumerator = store.GetAllAsync<TestObjectA>().GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            storedCollection.Add(enumerator.Current);
        }

        Assert.Equal(collection.OrderBy(x => x.Id).AsString(), storedCollection.OrderBy(x => x.Id).AsString());
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task CanDeleteAllDocumentsOfATypeAtOnce(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var collection = _fixture.CreateMany<TestObjectA>();

        foreach (var obj in collection)
        {
            await store.CreateAsync(obj);
        }

        await store.DeleteAllAsync<TestObjectA>();

        var storedCollection = new List<TestObjectA>();
        var enumerator = store.GetAllAsync<TestObjectA>().GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            storedCollection.Add(enumerator.Current);
        }

        Assert.Empty(storedCollection);
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task ThrowsIfDocumentAlreadyExists(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var obj1 = _fixture.Create<TestObjectA>();
        var obj2 = _fixture.Create<TestObjectA>() with { Id = obj1.Id };

        await store.CreateAsync(obj1);
        await Assert.ThrowsAsync<AlreadyExistException>(async () => await store.CreateAsync(obj2));
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task ThrowsIfDocumentNotExists(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);
        await store.DeleteAllAsync<TestObjectA>();

        var obj = _fixture.Create<TestObjectA>();

        await Assert.ThrowsAsync<NotFoundException>(async () => await store.UpdateAsync(obj));
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task ThrowsIfDocumentTypeNotRegistered(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var store = documentStoreFactory(_config);

        var obj = _fixture.Create<TestObjectC>();

        await Assert.ThrowsAsync<TypeNotRegisteredException>(async () => await store.UpdateAsync(obj));
    }

    [Theory]
    [MemberData(nameof(DocumentStoreFactory))]
    public async Task UsesConfiguredSerializer(Func<DocumentStoreConfiguration, IDocumentStore> documentStoreFactory)
    {
        var serializer = new FakeSerializer();
        _config.Serializer = serializer;
        var store = documentStoreFactory(_config);

        var obj1 = _fixture.Create<TestObjectA>();
        var obj2 = _fixture.Create<TestObjectA>();

        object? objToSerialize = null;
        string? strToDeserialize = null;
        serializer.OnSerialize = x =>
        {
            objToSerialize = x;
            return "{\"value\": \"fake\"}";
        };
        serializer.OnDeserialize = x =>
        {
            strToDeserialize = x;
            return obj2;
        };

        await store.CreateAsync(obj1);
        var storedObj = await store.GetByIdAsync<TestObjectA>(obj1.Id);

        Assert.Equal(obj1.AsString(), objToSerialize.AsString());
        Assert.Equal("{\"value\": \"fake\"}", strToDeserialize);
        Assert.Equal(obj2?.AsString(), storedObj.AsString());
    }

    public static IEnumerable<object[]> DocumentStoreFactory
    {
        get
        {
            yield return new object[] { new Func<DocumentStoreConfiguration, IDocumentStore>(config => _inMemoryStoreFixture!.CreateDocumentStore(config)) };
            yield return new object[] { new Func<DocumentStoreConfiguration, IDocumentStore>(config => _fileStoreFixture!.CreateDocumentStore(config)) };
            yield return new object[] { new Func<DocumentStoreConfiguration, IDocumentStore>(config => _postgreSqlStoreFixture!.CreateDocumentStore(config)) };
            yield return new object[] { new Func<DocumentStoreConfiguration, IDocumentStore>(config => _sqlServerStoreFixture!.CreateDocumentStore(config)) };
        }
    }

    internal record TestObjectA(int Id, string Text, List<TestSubObjectA> SubObjects);

    internal record TestSubObjectA(int Id, string Text);

    internal record TestObjectB(Guid Id, TestSubObjectB[] Values);

    internal record TestSubObjectB(string A, string B, string C, string D, string E);

    internal record TestObjectC(int Id, string Text);
}
