using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class GetInterviews
{
    public sealed record SessionDto(
        int InterviewSessionId,
        int UserId,
        string UserName,
        string TrackName,
        string SeniorityLevel,
        decimal Score,
        DateTime StartDate,
        DateTime? EndDate,
        string? Feedback
    );

    public sealed record Request(int PageIndex = 1, int PageSize = 10)
        : IRequest<Result<PaginatedResult<SessionDto>>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public sealed class Handler(AppDbContext context)
        : IRequestHandler<Request, Result<PaginatedResult<SessionDto>>>
    {
        public async Task<Result<PaginatedResult<SessionDto>>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var query = context
                .InterviewSessions.AsNoTracking()
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SessionDto(
                    s.Id,
                    s.UserId,
                    s.User.UserName,
                    s.TrackName,
                    s.SeniorityLevel,
                    s.Score,
                    s.StartDate,
                    s.EndDate,
                    s.Feedback
                ));

            var totalCount = await query.CountAsync(cancellationToken);

            var sessions = await query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<SessionDto>(
                sessions,
                totalCount,
                request.PageIndex,
                request.PageSize
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/interview-sessions",
                    async ([AsParameters] Request request, IMediator mediator) =>
                    {
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Interviews")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
