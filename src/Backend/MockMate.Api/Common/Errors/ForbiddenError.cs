using System.Net;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Common.Errors;

[HttpCode(HttpStatusCode.Forbidden)]
public sealed class ForbiddenError(string message = "Forbidden") : DomainError
{
    public override string Code => "forbidden";
    public override string Message => message;
}
