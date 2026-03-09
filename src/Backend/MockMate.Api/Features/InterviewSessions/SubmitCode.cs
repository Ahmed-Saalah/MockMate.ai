using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Extensions;
using MockMate.Api.Services.CodeExecutionService;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class SubmitCode
{
    public sealed record Request(int QuestionId, int LanguageId, string SourceCode)
        : IRequest<Result<Response>>
    {
        [JsonIgnore]
        public int InterviewSessionId { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }

    public sealed record Response(
        int SessionAnswerId,
        string Status,
        decimal Score,
        int PassedTestCases,
        int TotalTestCases,
        List<TestCaseDetail> TestCaseResults
    );

    public sealed record TestCaseDetail(
        int TestCaseId,
        string Input,
        string ExpectedOutput,
        string? ActualOutput,
        string? CompileOutput,
        string Status
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

    public sealed class Handler(AppDbContext context, ICodeExecutionService executionService)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var session = await context
                .InterviewSessions.Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == request.InterviewSessionId, cancellationToken);

            if (session is null)
            {
                return new NotFoundError("Interview session not found.");
            }

            if (session.UserId != request.UserId)
            {
                return new ForbiddenError("You do not have access to this interview session.");
            }

            var answer = session.Answers.FirstOrDefault(a => a.QuestionId == request.QuestionId);
            if (answer is null)
            {
                return new NotFoundError(
                    "The specified question is not part of this interview session."
                );
            }

            var question = await context
                .Questions.Include(q => q.TestCases.OrderBy(tc => tc.Id))
                .Include(q => q.Templates)
                .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

            if (question is null)
            {
                return new NotFoundError("Question not found.");
            }

            var template = question.Templates.FirstOrDefault(t =>
                t.LanguageId == request.LanguageId
            );
            if (template is null)
            {
                return new NotFoundError(
                    $"No code template exists for language ID {request.LanguageId} on this question."
                );
            }

            var testCases = question.TestCases.ToList();
            if (testCases.Count == 0)
            {
                return new BadRequestError("This question has no test cases configured.");
            }

            CodeExecutionResult executionResult;
            try
            {
                executionResult = await executionService.ExecuteAsync(
                    request.SourceCode,
                    request.LanguageId,
                    template,
                    testCases,
                    cancellationToken
                );
            }
            catch (Exception)
            {
                return new ServiceUnavailableError();
            }

            string overallStatus;
            decimal score;

            if (executionResult.HasCompilationError)
            {
                overallStatus = ExecutionStatus.CompilationError;
                score = 0m;
            }
            else
            {
                bool allPassed = executionResult.PassedCount == executionResult.TotalCount;
                overallStatus = allPassed ? ExecutionStatus.Passed : ExecutionStatus.Failed;
                score =
                    executionResult.TotalCount > 0
                        ? Math.Round(
                            (decimal)executionResult.PassedCount / executionResult.TotalCount * 100,
                            2
                        )
                        : 0m;
            }

            answer.SubmittedCode = request.SourceCode;
            answer.LanguageId = request.LanguageId;
            answer.Score = score;
            answer.Status = overallStatus;
            answer.IsCorrect = overallStatus == ExecutionStatus.Passed;

            await context.SaveChangesAsync(cancellationToken);

            var testCaseDetails = executionResult
                .Details.Select(d => new TestCaseDetail(
                    d.TestCaseId,
                    d.IsHidden ? ExecutionStatus.HiddenTestCase : d.Input,
                    d.IsHidden ? ExecutionStatus.Hidden : d.ExpectedOutput,
                    d.IsHidden ? null : d.ActualOutput,
                    d.IsHidden ? null : d.CompileOutput,
                    d.Status
                ))
                .ToList();

            return new Response(
                answer.Id,
                overallStatus,
                score,
                executionResult.PassedCount,
                executionResult.TotalCount,
                testCaseDetails
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/interview-sessions/{id:int}/submit-code",
                    async (int id, Request body, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();
                        if (string.IsNullOrEmpty(userId))
                            return Results.Unauthorized();

                        var request = body with
                        {
                            InterviewSessionId = id,
                            UserId = int.Parse(userId),
                        };
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Interviews")
                .RequireAuthorization();
        }
    }
}
