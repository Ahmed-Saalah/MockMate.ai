using System.Net;

namespace MockMate.Api.Common.Http;

[AttributeUsage(AttributeTargets.Class)]
public class HttpCodeAttribute : Attribute
{
    public HttpStatusCode Code { get; protected set; }

    public HttpCodeAttribute(HttpStatusCode code)
    {
        Code = code;
    }
}
