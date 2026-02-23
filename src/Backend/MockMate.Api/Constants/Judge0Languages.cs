namespace MockMate.Api.Constants;

public static class Judge0Languages
{
    public static readonly Dictionary<int, string> Supported = new()
    {
        { 51, "C#" },
        { 54, "C++" },
        { 71, "Python" },
        { 62, "Java" },
    };

    public static string GetName(int id) => Supported.GetValueOrDefault(id, "Unknown Language");
}
