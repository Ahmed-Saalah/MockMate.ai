using System.Text.Json.Serialization;

namespace MockMate.Api.Clients.Judge0.Dtos;

/// <summary>
/// Represents a single token returned immediately after a batch submission POST.
/// </summary>
public record Judge0SubmissionToken(
    [property: JsonPropertyName("token")] string Token
);

/// <summary>
/// Judge0 status object nested inside each submission result.
/// </summary>
public record Judge0Status(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("description")] string Description
);

/// <summary>
/// Full result for a single submission, returned while polling.
/// </summary>
public record Judge0SubmissionResult
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public Judge0Status? Status { get; init; }

    [JsonPropertyName("stdout")]
    public string? Stdout { get; init; }

    [JsonPropertyName("stderr")]
    public string? Stderr { get; init; }

    [JsonPropertyName("compile_output")]
    public string? CompileOutput { get; init; }
}

/// <summary>
/// Envelope returned by the batch polling endpoint.
/// </summary>
public record Judge0BatchResultResponse(
    [property: JsonPropertyName("submissions")] List<Judge0SubmissionResult> Submissions
);
