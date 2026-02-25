namespace MockMate.Api.Common.Errors;

public interface IDomainError
{
    string Code { get; }
    string Message { get; }
}

public abstract class DomainError : IDomainError
{
    public abstract string Code { get; }
    public abstract string Message { get; }
}
