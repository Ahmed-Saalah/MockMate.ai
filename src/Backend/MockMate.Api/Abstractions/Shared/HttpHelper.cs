using System.Net;
using System.Reflection;

namespace MockMate.Api.Abstractions.Shared;

public static class HttpHelper
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.Error is null)
        {
            return Results.Ok(result.Value);
        }

        var code = result?.Error?.GetType().GetCustomAttribute<HttpCodeAttribute>()?.Code;

        return code switch
        {
            HttpStatusCode.BadRequest => Results.BadRequest(result?.Error),
            HttpStatusCode.NotFound => Results.NotFound(result?.Error),
            HttpStatusCode.Conflict => Results.Conflict(result?.Error),
            HttpStatusCode.Unauthorized => Results.Json(
                result?.Error,
                statusCode: StatusCodes.Status401Unauthorized
            ),
            HttpStatusCode.Forbidden => Results.Json(
                result?.Error,
                statusCode: StatusCodes.Status403Forbidden
            ),
            _ => Results.BadRequest(result?.Error),
        };
    }
}
