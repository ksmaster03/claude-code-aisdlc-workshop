namespace OpsTrack.Business.Common;

/// <summary>Bad input from the caller. Mapped to HTTP 400.</summary>
public class ValidationFailedException : Exception
{
    public ValidationFailedException(string field, string message)
        : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}

/// <summary>The requested record does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

/// <summary>The record is in a state that does not allow the action. Mapped to HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
