using System.Net;

namespace MockMate.Api.Abstractions.Shared;

[AttributeUsage(AttributeTargets.Class)]
public class HttpCodeAttribute : Attribute
{
    public HttpStatusCode Code { get; protected set; }

    public HttpCodeAttribute(HttpStatusCode code)
    {
        Code = code;
    }
}
