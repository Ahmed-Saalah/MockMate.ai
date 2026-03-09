using MockMate.Api.Clients.Judge0.Interfaces;
using MockMate.Api.Constants;
using MockMate.Api.Entities;

namespace MockMate.Api.Services.CodeExecutionService;

public sealed class CodeExecutionService(IJudge0Service judge0Service) : ICodeExecutionService
{
    // Judge0 status ID for "Accepted" (code ran without error).
    private const int Judge0Accepted = 3;
    private const int Judge0CompilationError = 6;

    public async Task<CodeExecutionResult> ExecuteAsync(
        string sourceCode,
        int languageId,
        LanguageTemplate template,
        IReadOnlyList<TestCase> testCases,
        CancellationToken cancellationToken = default
    )
    {
        var mergedCode = MergeCode(sourceCode, template.DriverCode);

        var rawResults = await judge0Service.ExecuteAsync(
            testCases,
            languageId,
            mergedCode,
            template.TimeLimit,
            template.MemoryLimit,
            cancellationToken
        );

        var details = new List<ExecutionResultDetail>(testCases.Count);
        int passedCount = 0;

        for (int i = 0; i < testCases.Count; i++)
        {
            var tc = testCases[i];
            var raw = rawResults[i];

            bool isCompilationError = raw.Status?.Id == Judge0CompilationError;

            bool isCorrect =
                !isCompilationError
                && raw.Status?.Id == Judge0Accepted
                && string.Equals(raw.Stdout?.Trim(), tc.Output.Trim(), StringComparison.Ordinal);

            if (isCorrect)
                passedCount++;

            string status = raw.Status?.Id switch
            {
                Judge0Accepted => isCorrect
                    ? ExecutionStatus.Accepted
                    : ExecutionStatus.WrongAnswer,
                Judge0CompilationError => ExecutionStatus.CompilationError,
                _ => raw.Status?.Description ?? ExecutionStatus.WrongAnswer,
            };

            details.Add(
                new ExecutionResultDetail(
                    TestCaseId: tc.Id,
                    Input: tc.Input,
                    ExpectedOutput: tc.Output,
                    ActualOutput: raw.Stdout?.Trim(),
                    CompileOutput: raw.CompileOutput,
                    Status: status,
                    IsCorrect: isCorrect,
                    IsCompilationError: isCompilationError,
                    IsHidden: tc.IsHidden
                )
            );
        }

        bool hasCompilationError = details.Any(d => d.IsCompilationError);

        return new CodeExecutionResult(
            HasCompilationError: hasCompilationError,
            PassedCount: passedCount,
            TotalCount: testCases.Count,
            Details: details
        );
    }

    /// <summary>
    /// Injects <paramref name="sourceCode"/> into the driver code via the
    /// <c>{{USER_CODE}}</c> placeholder. Falls back to using the source code as-is
    /// when no driver code is configured for the template.
    /// </summary>
    private static string MergeCode(string sourceCode, string? driverCode) =>
        string.IsNullOrWhiteSpace(driverCode)
            ? sourceCode
            : driverCode.Replace("{{USER_CODE}}", sourceCode);
}
