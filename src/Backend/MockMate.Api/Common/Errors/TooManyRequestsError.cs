using System.Net;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Common.Errors;

[HttpCode(HttpStatusCode.TooManyRequests)]
public class TooManyRequestsError : DomainError
{
    public override string Code => "too_many_requests";

    public override string Message { get; }

    public TooManyRequestsError(string message = "Too many requests. Please try again later.")
    {
        Message = message;
    }
}
