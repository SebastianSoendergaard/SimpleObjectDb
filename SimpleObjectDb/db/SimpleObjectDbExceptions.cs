namespace SimpleFileDatabase;

public class SimpleFileDbException : Exception
{
    public SimpleFileDbException(string message) : base(message) { }
    public SimpleFileDbException(string message, Exception exception) : base(message, exception) { }
}

public class TypeNotRegisteredException : SimpleFileDbException
{
    public TypeNotRegisteredException(string message) : base(message) { }
}

public class AlreadyExistException : SimpleFileDbException
{
    public AlreadyExistException(string message) : base(message) { }
}

public class NotFoundException : SimpleFileDbException
{
    public NotFoundException(string message) : base(message) { }
}
