using System.Reflection;
using AutoFixture;
using Basses.SimpleDocumentStore;
using Basses.SimpleDocumentStore.Files;
using Newtonsoft.Json;

namespace SimpleObjectDb;

internal static class Serialization
{
    static Fixture _fixture = new();

    public static async Task Run()
    {
        await SerializeWithDefault();
        await SerializeWithDefaultButCustomOptions();
        await SerializeWithCustomSerializer();
    }

    private static async Task SerializeWithDefault()
    {
        Console.WriteLine("Serialize using default serializer");

        DocumentStoreConfiguration config = new();
        config.RegisterDataType<TestObjectA>(i => i.Id);
        IDocumentStore store = new FileDocumentStore(Constants.FileConnectionString, config);

        var obj = _fixture.Create<TestObjectA>();
        await store.CreateAsync(obj);
        var storedObject = await store.GetByIdAsync<TestObjectA>(obj.Id);

        var filePath = Path.Combine(Constants.FileConnectionString, nameof(TestObjectA), obj.Id.ToString() + ".json");
        var json = File.ReadAllText(filePath);

        Console.WriteLine($"Object saved to '{filePath}', with content:");
        Console.WriteLine(json);
        Console.WriteLine("");
    }

    private static async Task SerializeWithDefaultButCustomOptions()
    {
        Console.WriteLine("Serialize using default serializer but custom options");

        DocumentStoreConfiguration config = new();
        config.RegisterDataType<TestObjectA>(i => i.Id);
        var serializer = config.Serializer as DefaultDocumentStoreSerializer;
        if (serializer != null)
        {
            serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
            serializer.Options.WriteIndented = true;
        }
        IDocumentStore store = new FileDocumentStore(Constants.FileConnectionString, config);

        var obj = _fixture.Create<TestObjectA>();
        await store.CreateAsync(obj);
        var storedObject = await store.GetByIdAsync<TestObjectA>(obj.Id);

        var filePath = Path.Combine(Constants.FileConnectionString, nameof(TestObjectA), obj.Id.ToString() + ".json");
        var json = File.ReadAllText(filePath);

        Console.WriteLine($"Object saved to '{filePath}', with content:");
        Console.WriteLine(json);
        Console.WriteLine("");
    }

    private static async Task SerializeWithCustomSerializer()
    {
        Console.WriteLine("Serialize using custom serializer");

        DocumentStoreConfiguration config = new();
        config.RegisterDataType<TestObjectWithEncapsulatedData>(i => i.Id);
        config.Serializer = new EncapsulatedPropertiesSerializer();
        IDocumentStore store = new FileDocumentStore(Constants.FileConnectionString, config);

        var obj = new TestObjectWithEncapsulatedData(Guid.NewGuid(), Guid.NewGuid().ToString());
        obj.SetPrivateText(Guid.NewGuid().ToString());
        await store.CreateAsync(obj);
        var storedObject = await store.GetByIdAsync<TestObjectWithEncapsulatedData>(obj.Id);

        var filePath = Path.Combine(Constants.FileConnectionString, nameof(TestObjectWithEncapsulatedData), obj.Id.ToString() + ".json");
        var json = File.ReadAllText(filePath);

        Console.WriteLine($"Object saved to '{filePath}', with content:");
        Console.WriteLine(json);
        Console.WriteLine("");
    }

    /// <summary>
    /// Custom serializer that serializes all properties (public and private) with a setter. This way objects can encapsulate information and still be serialized without pollution of special attributes. 
    /// </summary>
    public class EncapsulatedPropertiesSerializer : IDocumentStoreSerializer
    {
        Newtonsoft.Json.JsonSerializerSettings _settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            ContractResolver = new PropertiesContractResolver()
        };

        public Tdata FromJson<Tdata>(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Tdata>(data, _settings) ?? throw new DeserializeException($"Can not deserialize to type", typeof(Tdata), data);
        }

        public string ToJson<Tdata>(Tdata data)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented, _settings);
        }

        public class PropertiesContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p?.SetMethod != null)
                    .Select(p => base.CreateProperty(p, memberSerialization))
                    .ToList();

                properties.ForEach(p => { p.Writable = true; p.Readable = true; });

                return properties;
            }
        }
    }
}
