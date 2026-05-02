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

namespace MockMate.Api.Features.InterviewSessions;

public sealed class SubmitLiveInterview
{
    public sealed record Request(decimal Score, string? Feedback) : IRequest<Result<Response>>
    {
        [JsonIgnore]
        public int Id { get; set; }
    }

    public sealed record Response(decimal Score, string? Feedback);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Score).InclusiveBetween(0, 100).WithMessage("Score must be between 0 and 100.");
        }
    }

    public sealed class Handler(AppDbContext context) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var interview = await context
                .InterviewSessions
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (interview is null)
                return new NotFoundError("Interview session not found");

            if (interview.EndDate != null)
                return new BadRequestError("Interview already submitted");

            if (interview.InterviewType != InterviewTypes.Live)
                return new BadRequestError("This is not a voice interview session");

            interview.Score = request.Score;
            interview.Feedback = request.Feedback;
            interview.EndDate = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new Response(interview.Score, interview.Feedback);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/interview-sessions/{id:int}/live/submit",
                    async (int id, Request request, IMediator mediator) =>
                    {
                        request.Id = id;
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags("Interviews");
        }
    }
}