using MockMate.Api.Entities;

namespace MockMate.Api.Services.CodeExecutionService;

public interface ICodeExecutionService
{
    /// <summary>
    /// Merges <paramref name="sourceCode"/> into the template's driver code, submits all
    /// <paramref name="testCases"/> to Judge0 as a batch job, and returns the mapped results.
    /// The returned <see cref="ExecutionResultDetail"/> list is ordered identically to
    /// <paramref name="testCases"/> so callers can rely on index alignment.
    /// </summary>
    Task<CodeExecutionResult> ExecuteAsync(
        string sourceCode,
        int languageId,
        LanguageTemplate template,
        IReadOnlyList<TestCase> testCases,
        CancellationToken cancellationToken = default
    );
}

public record CodeExecutionResult(
    bool HasCompilationError,
    int PassedCount,
    int TotalCount,
    IReadOnlyList<ExecutionResultDetail> Details
);

public record ExecutionResultDetail(
    int TestCaseId,
    string Input,
    string ExpectedOutput,
    string? ActualOutput,
    string? CompileOutput,
    string Status,
    bool IsCorrect,
    bool IsCompilationError,
    bool IsHidden
);
