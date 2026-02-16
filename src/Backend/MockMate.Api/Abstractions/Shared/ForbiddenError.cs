using System.Net;

namespace MockMate.Api.Abstractions.Shared;

[HttpCode(HttpStatusCode.Forbidden)]
public sealed class ForbiddenError(string message = "Forbidden") : DomainError
{
    public override string Code => "forbidden";
    public override string Message => message;
}
