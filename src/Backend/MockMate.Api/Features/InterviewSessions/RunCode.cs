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

public sealed class RunCode
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
        string Status,
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

            var answerExists = session.Answers.Any(a => a.QuestionId == request.QuestionId);
            if (!answerExists)
            {
                return new NotFoundError(
                    "The specified question is not part of this interview session."
                );
            }

            var question = await context
                .Questions.Include(q => q.TestCases.Where(tc => !tc.IsHidden).OrderBy(tc => tc.Id))
                .Include(q => q.Templates)
                .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

            if (question is null)
                return new NotFoundError("Question not found.");

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
                return new BadRequestError("This question has no visible test cases configured.");
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

            string overallStatus = executionResult.HasCompilationError
                ? ExecutionStatus.CompilationError
                : (
                    executionResult.PassedCount == executionResult.TotalCount
                        ? ExecutionStatus.Passed
                        : ExecutionStatus.Failed
                );

            var testCaseDetails = executionResult
                .Details.Select(d => new TestCaseDetail(
                    d.TestCaseId,
                    d.Input,
                    d.ExpectedOutput,
                    d.ActualOutput,
                    d.CompileOutput,
                    d.Status
                ))
                .ToList();

            return new Response(
                overallStatus,
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
                    "/interview-sessions/{id:int}/run-code",
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
