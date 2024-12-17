namespace SimpleObjectDb.db;

public class SimpleObjectDbException : Exception
{
    public SimpleObjectDbException(string message) : base(message) { }
    public SimpleObjectDbException(string message, Exception exception) : base(message, exception) { }
}

public class TypeNotRegisteredException : SimpleObjectDbException
{
    public TypeNotRegisteredException(string message) : base(message) { }
}

public class AlreadyExistException : SimpleObjectDbException
{
    public AlreadyExistException(string message) : base(message) { }
}

public class NotFoundException : SimpleObjectDbException
{
    public NotFoundException(string message) : base(message) { }
}
