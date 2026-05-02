using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;
using MockMate.Api.Extensions;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class CreateLiveInterview
{
    public sealed record Request(string TrackName) : IRequest<Result<Response>>
    {
        [JsonIgnore]
        public int UserId { get; set; }
    }

    public sealed record Response(int InterviewSessionId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TrackName).NotEmpty().WithMessage("Track name is required.");
        }
    }

    public sealed class Handler(AppDbContext context) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var session = new InterviewSession
            {
                UserId = request.UserId,
                TrackName = request.TrackName,
                SeniorityLevel = SeniorityLevels.General,
                InterviewType = InterviewTypes.Live,
                StartDate = DateTime.UtcNow,
            };

            context.InterviewSessions.Add(session);
            await context.SaveChangesAsync(cancellationToken);

            return new Response(session.Id);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/interview-sessions/live",
                    async (Request request, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();
                        if (string.IsNullOrEmpty(userId))
                            return Results.Unauthorized();

                        request.UserId = int.Parse(userId);
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Interviews")
                .RequireAuthorization();
        }
    }
}