using System.Net;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Common.Errors;

[HttpCode(HttpStatusCode.Conflict)]
public class ConflictError(string message = "Conflict") : DomainError
{
    public override string Code => "conflict";

    public override string Message => message;
}
