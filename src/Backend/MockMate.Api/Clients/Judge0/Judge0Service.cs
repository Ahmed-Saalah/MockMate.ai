using System.Net.Http.Json;
using System.Text.Json;
using MockMate.Api.Clients.Judge0.Dtos;
using MockMate.Api.Clients.Judge0.Exceptions;
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

        try
        {
            var postResponse = await httpClient.PostAsJsonAsync(
                "submissions/batch?base64_encoded=false",
                new Judge0BatchSubmissionRequest(submissions),
                cancellationToken
            );

            if (postResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new Judge0TooManyRequestsException(
                    "Code execution engine is currently at capacity. Please try again later."
                );
            }

            if (!postResponse.IsSuccessStatusCode)
            {
                throw new Judge0ServiceException(
                    $"Execution engine returned an error: {postResponse.ReasonPhrase} ({(int)postResponse.StatusCode}). Please try again."
                );
            }

            var tokens =
                await postResponse.Content.ReadFromJsonAsync<List<Judge0SubmissionToken>>(
                    JsonOptions,
                    cancellationToken
                ) ?? [];

            if (tokens.Count == 0)
            {
                throw new Judge0ServiceException(
                    "No submission tokens were returned by the execution engine. Please try again."
                );
            }

            var tokenStrings = tokens.Select(t => t.Token).ToList();
            var results = await PollUntilDoneAsync(tokenStrings, cancellationToken);

            // Ensure the result list count strictly matches the requested testCases count.
            if (results.Count < testCases.Count)
            {
                var alignedResults = new List<Judge0SubmissionResult>(testCases.Count);
                for (int i = 0; i < testCases.Count; i++)
                {
                    if (i < results.Count)
                    {
                        alignedResults.Add(results[i]);
                    }
                    else
                    {
                        alignedResults.Add(new Judge0SubmissionResult
                        {
                            Status = new Judge0Status(
                                13,
                                "Execution Engine Error: Missing Result"
                            )
                        });
                    }
                }
                return alignedResults;
            }

            return results;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            string errorMessage = ex switch
            {
                OperationCanceledException => "Code execution timed out. Please try again.",
                _ => "Unable to connect to the code execution engine. Please check your internet connection and try again."
            };

            throw new Judge0ServiceException(errorMessage, ex);
        }
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

        // Keep track of consecutive failures to avoid infinite loop on persistent issues
        int consecutiveFailures = 0;
        const int maxConsecutiveFailures = 5;

        while (true)
        {
            await Task.Delay(1_500, cancellationToken);

            Judge0BatchResultResponse? batch = null;
            try
            {
                using var response = await httpClient.GetAsync(pollUrl, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    consecutiveFailures++;
                    if (consecutiveFailures > maxConsecutiveFailures)
                    {
                        throw new Judge0TooManyRequestsException(
                            "Code execution engine is currently at capacity. Please try again later."
                        );
                    }
                    await Task.Delay(3_000, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    consecutiveFailures++;
                    if (consecutiveFailures > maxConsecutiveFailures)
                    {
                        throw new Judge0ServiceException(
                            $"Polling error: {response.ReasonPhrase} ({(int)response.StatusCode}). Please try again."
                        );
                    }
                    await Task.Delay(2_000 * consecutiveFailures, cancellationToken);
                    continue;
                }

                batch = await response.Content.ReadFromJsonAsync<Judge0BatchResultResponse>(
                    JsonOptions,
                    cancellationToken
                );

                consecutiveFailures = 0; // Reset on successful fetch
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
            {
                consecutiveFailures++;
                if (consecutiveFailures > maxConsecutiveFailures)
                {
                    string msg = ex switch
                    {
                        OperationCanceledException => "Polling timed out. Please try again.",
                        _ => "Unable to retrieve execution results. Please check your network and try again."
                    };
                    throw new Judge0ServiceException(msg, ex);
                }

                // Exponential back-off: wait longer before retrying
                await Task.Delay(2_000 * consecutiveFailures, cancellationToken);
                continue;
            }

            if (batch?.Submissions is null)
            {
                consecutiveFailures++;
                if (consecutiveFailures > maxConsecutiveFailures)
                {
                    throw new Judge0ServiceException(
                        "Execution engine returned invalid response structure. Please try again."
                    );
                }
                await Task.Delay(2_000, cancellationToken);
                continue;
            }

            var stillRunning = batch.Submissions.Any(s =>
                s is null || s.Status is null || ProcessingStatuses.Contains(s.Status.Id)
            );

            if (!stillRunning)
            {
                // Align submissions with original requested tokens list order to ensure
                // that output matches test cases correctly by index.
                var submissionMap = batch
                    .Submissions.Where(s => s is not null && s.Token is not null)
                    .ToDictionary(s => s.Token);

                return tokens
                    .Select(t =>
                        submissionMap.TryGetValue(t, out var res)
                            ? res
                            : new Judge0SubmissionResult
                            {
                                Token = t,
                                Status = new Judge0Status(
                                    13,
                                    "Internal Error (Submission Missing)"
                                ),
                            }
                    )
                    .ToList();
            }
        }
    }
}
