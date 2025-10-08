namespace Basses.SimpleDocumentStore.Tests;

internal class FakeSerializer : IDocumentStoreSerializer
{
    public Func<object, string> OnSerialize { get; set; } = _ => "";
    public Func<string, object> OnDeserialize { get; set; } = _ => "";

    public Tdata FromJson<Tdata>(string data)
    {
        return (Tdata)OnDeserialize(data);
    }

    public string ToJson<Tdata>(Tdata data)
    {
        return OnSerialize(data!);
    }
}
