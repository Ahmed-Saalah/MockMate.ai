using System.Net;

namespace MockMate.Api.Abstractions.Shared;

[HttpCode(HttpStatusCode.BadRequest)]
public class MultipleError : DomainError
{
    public override string Code => "multiple_error";

    public IEnumerable<IDomainError> Errors { get; }

    public override string Message { get; }

    public MultipleError(IEnumerable<IDomainError> errors)
    {
        Errors = errors;
    }
}
