using System.Net;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Common.Errors;

[HttpCode(HttpStatusCode.BadRequest)]
public class BadRequestError(string message = "Bad request") : DomainError
{
    public override string Code => "bad_request";

    public override string Message => message;
}
