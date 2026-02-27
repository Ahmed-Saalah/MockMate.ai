using System.Text.Json.Serialization;

namespace MockMate.Api.Clients.AiService.Dtos;

public class CandidateProfileResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public CandidateProfileData Data { get; init; } = null!;
}

public class CandidateProfileData
{
    [JsonPropertyName("track_name")]
    public string TrackName { get; init; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; init; } = string.Empty;

    [JsonPropertyName("technical_skills")]
    public List<string> TechnicalSkills { get; init; } = [];
}
