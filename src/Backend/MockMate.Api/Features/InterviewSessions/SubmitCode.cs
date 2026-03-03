using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Clients.Judge0.Interfaces;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Extensions;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class SubmitCode
{
    public sealed record Request(
        int InterviewSessionId,
        int QuestionId,
        int LanguageId,
        string SourceCode,
        bool IsFinalSubmit
    ) : IRequest<Result<Response>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }

    public sealed record Response(
        int SessionAnswerId,
        string Status,
        decimal Score,
        int PassedTestCases,
        int TotalTestCases
    );

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.QuestionId)
                .GreaterThan(0)
                .WithMessage("A valid QuestionId is required.");

            RuleFor(x => x.LanguageId)
                .GreaterThan(0)
                .WithMessage("A valid LanguageId is required.")
                .Must(id => Judge0Languages.Supported.ContainsKey(id))
                .WithMessage(x =>
                    $"Language ID {x.LanguageId} is not supported. "
                    + $"Supported IDs: {string.Join(", ", Judge0Languages.Supported.Keys)}."
                );

            RuleFor(x => x.SourceCode).NotEmpty().WithMessage("Source code cannot be empty.");
        }
    }

    public sealed class Handler(AppDbContext context, IJudge0Service judge0Service)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            // 1. Load the session and verify ownership.
            var session = await context
                .InterviewSessions.Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == request.InterviewSessionId, cancellationToken);

            if (session is null)
                return new NotFoundError("Interview session not found.");

            if (session.UserId != int.Parse(request.UserId))
                return new ForbiddenError("You do not have access to this interview session.");

            // 2. Find the pre-created SessionAnswer for this question.
            var answer = session.Answers.FirstOrDefault(a => a.QuestionId == request.QuestionId);
            if (answer is null)
                return new NotFoundError(
                    "The specified question is not part of this interview session."
                );

            // 3. Load question with its test cases and language templates.
            var question = await context
                .Questions.Include(q => q.TestCases)
                .Include(q => q.Templates)
                .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

            if (question is null)
                return new NotFoundError("Question not found.");

            var template = question.Templates.FirstOrDefault(t =>
                t.LanguageId == request.LanguageId
            );
            if (template is null)
                return new NotFoundError(
                    $"No code template exists for language ID {request.LanguageId} on this question."
                );

            // "Run Code" only sees visible test cases; "Submit Code" sees all of them.
            // The stable OrderBy ensures batch result indexes align perfectly with our local list.
            var testCases = question
                .TestCases.Where(tc => request.IsFinalSubmit || !tc.IsHidden)
                .OrderBy(tc => tc.Id)
                .ToList();

            if (testCases.Count == 0)
                return new BadRequestError("This question has no test cases configured.");

            // 4. Merge user code with the driver code stored in the database.
            var mergedCode = string.IsNullOrWhiteSpace(template.DriverCode)
                ? request.SourceCode
                : $"{request.SourceCode}\n{template.DriverCode}";

            // 5. Execute all test cases via Judge0 batch submission.
            var results = await judge0Service.ExecuteAsync(
                testCases,
                request.LanguageId,
                mergedCode,
                template.TimeLimit,
                template.MemoryLimit,
                cancellationToken
            );

            // 6. Evaluate results.
            // Judge0 status IDs: 3 = Accepted, 6 = Compilation Error, 7–12 = Runtime Error.
            var compilationError = results.FirstOrDefault(r => r.Status?.Id == 6);
            if (compilationError is not null)
            {
                if (request.IsFinalSubmit)
                {
                    UpdateAnswer(
                        answer,
                        request,
                        status: "Compilation Error",
                        score: 0,
                        isCorrect: false
                    );
                    await context.SaveChangesAsync(cancellationToken);
                }

                return new Response(answer.Id, "Compilation Error", 0, 0, testCases.Count);
            }

            int passedCount = results
                .Select((result, index) => (result, expected: testCases[index].Output))
                .Count(x =>
                    x.result.Status?.Id == 3
                    && string.Equals(
                        x.result.Stdout?.Trim(),
                        x.expected.Trim(),
                        StringComparison.Ordinal
                    )
                );

            int totalCount = testCases.Count;
            bool allPassed = passedCount == totalCount;
            string status = allPassed ? "Passed" : "Failed";
            decimal score =
                totalCount > 0 ? Math.Round((decimal)passedCount / totalCount * 100, 2) : 0m;

            // 7. Only persist the result on a final submission; "Run Code" is read-only.
            if (request.IsFinalSubmit)
            {
                UpdateAnswer(answer, request, status, score, isCorrect: allPassed);
                await context.SaveChangesAsync(cancellationToken);
            }

            return new Response(answer.Id, status, score, passedCount, totalCount);
        }

        private static void UpdateAnswer(
            Entities.SessionAnswer answer,
            Request request,
            string status,
            decimal score,
            bool isCorrect
        )
        {
            answer.SubmittedCode = request.SourceCode;
            answer.LanguageId = request.LanguageId;
            answer.Score = score;
            answer.Status = status;
            answer.IsCorrect = isCorrect;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/interview-sessions/{id:int}/submit-code",
                    async (int id, SubmitCodeBody body, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();
                        if (string.IsNullOrEmpty(userId))
                            return Results.Unauthorized();

                        var request = new Request(
                            id,
                            body.QuestionId,
                            body.LanguageId,
                            body.SourceCode,
                            body.IsFinalSubmit
                        )
                        {
                            UserId = userId,
                        };

                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Interviews")
                .RequireAuthorization();
        }
    }

    /// <summary>The JSON body for POST /interview-sessions/{id}/submit-code.</summary>
    /// <param name="IsFinalSubmit">
    /// Pass <c>false</c> to run against visible test cases only without saving ("Run Code").
    /// Pass <c>true</c> to grade against all test cases and persist the result ("Submit Code").
    /// </param>
    public sealed record SubmitCodeBody(
        int QuestionId,
        int LanguageId,
        string SourceCode,
        bool IsFinalSubmit
    );
}
