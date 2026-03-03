namespace MockMate.Api.Configuration;

public class Judge0Options
{
    public const string SectionName = "Judge0";

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// RapidAPI key — leave empty when using a self-hosted Judge0 instance.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// RapidAPI host header value — leave empty when using a self-hosted instance.
    /// </summary>
    public string? ApiHost { get; set; }
}
