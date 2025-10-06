namespace Basses.SimpleDocumentStore;

public class SimpleDocumentStoreException : Exception
{
    public SimpleDocumentStoreException(string message) : base(message) { }
    public SimpleDocumentStoreException(string message, Exception exception) : base(message, exception) { }
}

public class TypeNotRegisteredException : SimpleDocumentStoreException
{
    public TypeNotRegisteredException(string message) : base(message) { }
}

public class AlreadyExistException : SimpleDocumentStoreException
{
    public AlreadyExistException(string message) : base(message) { }
}

public class NotFoundException : SimpleDocumentStoreException
{
    public NotFoundException(string message) : base(message) { }
}
