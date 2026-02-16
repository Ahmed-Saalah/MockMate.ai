using System.Net;

namespace MockMate.Api.Abstractions.Shared;

[HttpCode(HttpStatusCode.NotFound)]
public class NotFound(string message = "Not found") : DomainError
{
    public override string Code => "not_found";

    public override string Message { get; } = message;
}
