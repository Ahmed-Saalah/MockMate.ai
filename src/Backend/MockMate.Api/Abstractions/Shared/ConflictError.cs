using System.Net;

namespace MockMate.Api.Abstractions.Shared;

[HttpCode(HttpStatusCode.Conflict)]
public class ConflictError(string message = "Conflict") : DomainError
{
    public override string Code => "conflict";

    public override string Message => message;
}
