using System.Net;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Common.Errors;

[HttpCode(HttpStatusCode.NotFound)]
public class NotFoundError(string message = "Not found") : DomainError
{
    public override string Code => "not_found";

    public override string Message { get; } = message;
}
