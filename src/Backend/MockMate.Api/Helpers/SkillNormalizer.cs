namespace MockMate.Api.Helpers;

public static class SkillNormalizer
{
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Removes spaces, dots, dashes, and slashes, then makes it lowercase.
        // "Node.js" -> "nodejs"
        // " C# " -> "c#"
        // "React-Native" -> "reactnative"
        // "CI/CD" -> "cicd"
        // "C++" -> "c++"
        return name.Replace(" ", "")
            .Replace(".", "")
            .Replace("-", "")
            .Replace("/", "")
            .ToLowerInvariant();
    }
}
