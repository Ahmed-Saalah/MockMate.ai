using System.Net;
using System.Reflection;
using MockMate.Api.Common.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace MockMate.Api.Common.Http;

public static class HttpHelper
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.Error is null)
        {
            return HttpResults.Ok(result.Value);
        }

        var code = result?.Error?.GetType().GetCustomAttribute<HttpCodeAttribute>()?.Code;

        return code switch
        {
            HttpStatusCode.BadRequest => HttpResults.BadRequest(result?.Error),
            HttpStatusCode.NotFound => HttpResults.NotFound(result?.Error),
            HttpStatusCode.Conflict => HttpResults.Conflict(result?.Error),
            HttpStatusCode.Unauthorized => HttpResults.Json(
                result?.Error,
                statusCode: StatusCodes.Status401Unauthorized
            ),
            HttpStatusCode.Forbidden => HttpResults.Json(
                result?.Error,
                statusCode: StatusCodes.Status403Forbidden
            ),
            _ => HttpResults.BadRequest(result?.Error),
        };
    }
}
