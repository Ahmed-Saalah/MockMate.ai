using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Data;
using MockMate.Api.Extensions;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class AnswerMcq
{
    public sealed record Request(int QuestionId, int SelectedOptionId) : IRequest<Result<Response>>
    {
        [JsonIgnore]
        public int InterviewSessionId { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }

    public sealed record Response(int SessionAnswerId, bool Success);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.QuestionId)
                .GreaterThan(0)
                .WithMessage("A valid QuestionId is required.");

            RuleFor(x => x.SelectedOptionId)
                .GreaterThan(0)
                .WithMessage("A valid SelectedOptionId is required.");
        }
    }

    public sealed class Handler(AppDbContext context) : IRequestHandler<Request, Result<Response>>
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
                return new NotFoundError("Interview session not found.");

            if (session.UserId != request.UserId)
                return new ForbiddenError("You do not have access to this interview session.");

            if (session.EndDate is not null)
                return new BadRequestError("This interview session has already been submitted.");

            var answer = session.Answers.FirstOrDefault(a => a.QuestionId == request.QuestionId);
            if (answer is null)
                return new NotFoundError(
                    "The specified question is not part of this interview session."
                );

            var question = await context
                .Questions.Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);

            if (question is null)
                return new NotFoundError("Question not found.");

            var option = question.Options.FirstOrDefault(o => o.Id == request.SelectedOptionId);
            if (option is null)
                return new BadRequestError("The selected option does not belong to this question.");

            answer.SelectedOptionId = request.SelectedOptionId;
            answer.IsCorrect = option.IsCorrect;
            answer.Score = option.IsCorrect ? 100 : 0;

            await context.SaveChangesAsync(cancellationToken);

            return new Response(answer.Id, Success: true);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "/interview-sessions/{id:int}/answer-mcq",
                    async (int id, Request body, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();
                        if (string.IsNullOrEmpty(userId))
                            return Results.Unauthorized();

                        var request = new Request(body.QuestionId, body.SelectedOptionId)
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
