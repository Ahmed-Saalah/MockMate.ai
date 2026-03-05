namespace MockMate.Api.Constants;

public static class ExecutionStatus
{
    public const string Passed = "Passed";
    public const string Failed = "Failed";
    public const string CompilationError = "Compilation Error";
    public const string Accepted = "Accepted";
    public const string WrongAnswer = "Wrong Answer";

    /// <summary>Replaces the Input field for hidden test cases in API responses.</summary>
    public const string HiddenTestCase = "Hidden Test Case";

    /// <summary>Replaces ExpectedOutput and ActualOutput fields for hidden test cases.</summary>
    public const string Hidden = "Hidden";
}
