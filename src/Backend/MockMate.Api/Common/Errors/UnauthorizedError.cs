using System.Net;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Common.Errors;

[HttpCode(HttpStatusCode.Unauthorized)]
public class UnauthorizedError(string message = "Unauthorized") : DomainError
{
    public override string Code => "unauthorized";

    public override string Message => message;
}
