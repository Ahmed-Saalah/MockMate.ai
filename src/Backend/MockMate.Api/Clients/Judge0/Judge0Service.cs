using System.Net.Http.Json;
using System.Text.Json;
using MockMate.Api.Clients.Judge0.Dtos;
using MockMate.Api.Clients.Judge0.Interfaces;
using MockMate.Api.Entities;

namespace MockMate.Api.Clients.Judge0;

public sealed class Judge0Service(HttpClient httpClient) : IJudge0Service
{
    // Judge0 status IDs 1 (In Queue) and 2 (Processing) are non-terminal.
    private static readonly HashSet<int> ProcessingStatuses = [1, 2];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<List<Judge0SubmissionResult>> ExecuteAsync(
        IReadOnlyList<TestCase> testCases,
        int languageId,
        string mergedCode,
        decimal timeLimit,
        int memoryLimitMb,
        CancellationToken cancellationToken = default
    )
    {
        var memoryLimitKb = memoryLimitMb * 1024;

        var submissions = testCases
            .Select(tc => new Judge0SubmissionItem(
                languageId,
                mergedCode,
                tc.Input,
                timeLimit,
                memoryLimitKb
            ))
            .ToList();

        var postResponse = await httpClient.PostAsJsonAsync(
            "submissions/batch?base64_encoded=false",
            new Judge0BatchSubmissionRequest(submissions),
            cancellationToken
        );

        if (postResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new Exception(
                "Code execution engine is currently at capacity. Please try again later."
            );
        }

        postResponse.EnsureSuccessStatusCode();

        var tokens =
            await postResponse.Content.ReadFromJsonAsync<List<Judge0SubmissionToken>>(
                JsonOptions,
                cancellationToken
            ) ?? [];

        if (tokens.Count == 0)
            return [];

        return await PollUntilDoneAsync(tokens.Select(t => t.Token).ToList(), cancellationToken);
    }

    private async Task<List<Judge0SubmissionResult>> PollUntilDoneAsync(
        List<string> tokens,
        CancellationToken cancellationToken
    )
    {
        var tokensCsv = string.Join(",", tokens);
        var pollUrl =
            $"submissions/batch?tokens={tokensCsv}&base64_encoded=false"
            + "&fields=token,status,stdout,stderr,compile_output";

        while (true)
        {
            await Task.Delay(1_500, cancellationToken);

            var batch = await httpClient.GetFromJsonAsync<Judge0BatchResultResponse>(
                pollUrl,
                JsonOptions,
                cancellationToken
            );

            if (batch is null)
                return [];

            var stillRunning = batch.Submissions.Any(s =>
                s.Status is null || ProcessingStatuses.Contains(s.Status.Id)
            );

            if (!stillRunning)
                return batch.Submissions;
        }
    }
}
