using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Data;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class GetUserInterviewSessions
{
    public sealed record SessionDto(
        int InterviewSessionId,
        decimal Score,
        DateTime StartDate,
        DateTime? EndDate,
        string? Feedback
    );

    public sealed record Request(
        int UserId,
        int PageIndex = 1,
        int PageSize = 10
    ) : IRequest<Result<PaginatedResult<SessionDto>>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public sealed class Handler(AppDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Result<PaginatedResult<SessionDto>>>
    {
        public async Task<Result<PaginatedResult<SessionDto>>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            var query = context.InterviewSessions
                .AsNoTracking()
                .Where(s => s.UserId == request.UserId)
                .OrderByDescending(s => s.StartDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var sessions = await query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new SessionDto(
                    s.Id,
                    s.Score,
                    s.StartDate,
                    s.EndDate,
                    s.Feedback
                ))
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
                    "/users/{userId:int}/interview-sessions",
                    async (
                        int userId,
                        int pageIndex,
                        int pageSize,
                        IMediator mediator) =>
                    {
                        var request = new Request(userId, pageIndex, pageSize);
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    })
                .WithTags("Interviews")
                .RequireAuthorization();
        }
    }
}