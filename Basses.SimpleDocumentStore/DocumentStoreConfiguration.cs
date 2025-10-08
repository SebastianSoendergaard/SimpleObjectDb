namespace Basses.SimpleDocumentStore;

public class DocumentStoreConfiguration
{
    public IDocumentStoreSerializer Serializer { get; set; } = new DefaultDocumentStoreSerializer();

    public Dictionary<Type, IIdConverter> IdConverters { get; } = [];
    public Dictionary<Type, string> Schemas { get; } = [];
    public DocumentStoreConfiguration RegisterDataType<Tdata>(Func<Tdata, object> dataTypeToIdValue, string? schema = null) where Tdata : class
    {
        var converter = new TypedIdConverter<Tdata>(dataTypeToIdValue);
        IdConverters.Add(typeof(Tdata), converter);
        if (schema != null)
        {
            Schemas.Add(typeof(Tdata), schema);
        }
        return this;
    }

    public interface IIdConverter { }

    public class TypedIdConverter<Tdata> : IIdConverter
    {
        private readonly Func<Tdata, object> _converter;

        public TypedIdConverter(Func<Tdata, object> converter)
        {
            _converter = converter;
        }

        public object GetId(Tdata data)
        {
            return _converter(data) ?? throw new ArgumentNullException($"Id for {typeof(Tdata).FullName} cannot be null");
        }
    }

    public object GetIdFromData<Tdata>(Tdata data) where Tdata : class
    {
        var type = typeof(Tdata);
        if (!IdConverters.TryGetValue(type, out var converter))
        {
            throw new TypeNotRegisteredException($"{type.FullName} not registered");
        }

        if (converter is not TypedIdConverter<Tdata> typedConverter)
        {
            throw new Exception();
        }

        return typedConverter.GetId(data);
    }

    public string? TryGetSchema(Type type)
    {
        return Schemas.TryGetValue(type, out var schema) ? schema : null;
    }
}
