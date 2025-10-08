namespace Basses.SimpleDocumentStore;

public class DocumentStoreException : Exception
{
    public DocumentStoreException(string message) : base(message) { }
    public DocumentStoreException(string message, Exception exception) : base(message, exception) { }
}

public class TypeNotRegisteredException : DocumentStoreException
{
    public TypeNotRegisteredException(string message) : base(message) { }
}

public class AlreadyExistException : DocumentStoreException
{
    public AlreadyExistException(string message) : base(message) { }
}

public class NotFoundException : DocumentStoreException
{
    public NotFoundException(string message) : base(message) { }
}

public class DeserializeException : DocumentStoreException
{
    public DeserializeException(string message, Type dataType, string data) : base(message) { }
}
