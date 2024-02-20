﻿namespace SimpleFileDatabase;

public class SimpleObjectDbConfiguration
{
    public Dictionary<Type, IIdConverter> IdConverters { get; } = new Dictionary<Type, IIdConverter>();
    public SimpleObjectDbConfiguration RegisterDataType<Tdata>(Func<Tdata, object> dataTypeToIdValue) where Tdata : class
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

        var typedConverter = converter as SimpleObjectDbConfiguration.TypedIdConverter<Tdata>;
        if (typedConverter == null)
        {
            throw new Exception();
        }

        return typedConverter.GetId(data);
    }
}
