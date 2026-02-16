using System.Net;

namespace MockMate.Api.Abstractions.Shared;

[HttpCode(HttpStatusCode.Unauthorized)]
public class UnauthorizedError(string message = "Unauthorized") : DomainError
{
    public override string Code => "unauthorized";

    public override string Message => message;
}
