using MockMate.Api.Clients.Judge0.Dtos;
using MockMate.Api.Entities;

namespace MockMate.Api.Clients.Judge0.Interfaces;

public interface IJudge0Service
{
    /// <summary>
    /// Submits <paramref name="mergedCode"/> against every test case as a Judge0 batch job,
    /// polls until all submissions reach a terminal state, and returns the ordered results.
    /// The results list preserves the same order as <paramref name="testCases"/>.
    /// </summary>
    Task<List<Judge0SubmissionResult>> ExecuteAsync(
        IReadOnlyList<TestCase> testCases,
        int languageId,
        string mergedCode,
        decimal timeLimit,
        int memoryLimitMb,
        CancellationToken cancellationToken = default
    );
}
