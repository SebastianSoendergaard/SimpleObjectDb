using System.Text.Json;

namespace Basses.SimpleDocumentStore;

public interface IDocumentStoreSerializer
{
    string ToJson<Tdata>(Tdata data);
    Tdata FromJson<Tdata>(string json);
}

public class DefaultDocumentStoreSerializer : IDocumentStoreSerializer
{
    public JsonSerializerOptions Options = new JsonSerializerOptions();

    public Tdata FromJson<Tdata>(string json)
    {
        return JsonSerializer.Deserialize<Tdata>(json, Options) ?? throw new DeserializeException($"Can not deserialize to type", typeof(Tdata), json);
    }

    public string ToJson<Tdata>(Tdata data)
    {
        return JsonSerializer.Serialize(data, Options);
    }
}
