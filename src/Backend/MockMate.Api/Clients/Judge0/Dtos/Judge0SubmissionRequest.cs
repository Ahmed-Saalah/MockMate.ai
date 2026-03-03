using System.Text.Json.Serialization;

namespace MockMate.Api.Clients.Judge0.Dtos;

public record Judge0SubmissionItem(
    [property: JsonPropertyName("language_id")] int LanguageId,
    [property: JsonPropertyName("source_code")] string SourceCode,
    [property: JsonPropertyName("stdin")] string Stdin,
    [property: JsonPropertyName("cpu_time_limit")] decimal CpuTimeLimit,
    [property: JsonPropertyName("memory_limit")] int MemoryLimit
);

public record Judge0BatchSubmissionRequest(
    [property: JsonPropertyName("submissions")] List<Judge0SubmissionItem> Submissions
);
