namespace Basses.SimpleDocumentStore;

public class DocumentStoreConfiguration
{
    public Dictionary<Type, IIdConverter> IdConverters { get; } = [];
    public DocumentStoreConfiguration RegisterDataType<Tdata>(Func<Tdata, object> dataTypeToIdValue) where Tdata : class
    {
        var converter = new TypedIdConverter<Tdata>(dataTypeToIdValue);
        IdConverters.Add(typeof(Tdata), converter);
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
}
