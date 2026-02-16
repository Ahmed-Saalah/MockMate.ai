using System.Net;

namespace MockMate.Api.Abstractions.Shared;

[HttpCode(HttpStatusCode.BadRequest)]
public class BadRequestError(string message = "Bad request") : DomainError
{
    public override string Code => "bad_request";

    public override string Message => message;
}
