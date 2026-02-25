using MockMate.Api.Common.Errors;

namespace MockMate.Api.Common.Results;

public class Result<T>
{
    public T? Value { get; protected set; }
    public IDomainError? Error { get; protected set; }

    public static implicit operator Result<T>(T value) => new() { Value = value };

    public static implicit operator Result<T>(DomainError error) => new() { Error = error };
}
