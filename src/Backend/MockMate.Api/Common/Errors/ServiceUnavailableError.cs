using System.Net;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Common.Errors;

[HttpCode(HttpStatusCode.ServiceUnavailable)]
public class ServiceUnavailableError : DomainError
{
    public override string Code => "service_unavailable";

    public override string Message =>
        "The service is currently unavailable. Please try again later.";
}
