namespace MockMate.Api.Abstractions.Shared;

public class ApplicationError : DomainError
{
    public ApplicationError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public override string Code { get; }
    public override string Message { get; }
}
