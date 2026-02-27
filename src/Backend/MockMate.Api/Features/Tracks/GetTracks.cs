using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Tracks;

public sealed class GetTracks
{
    public sealed record TrackDto(int Id, string Name, DateTime CreatedAt, int SkillCount);

    public sealed record Request(string? SearchTerm = null, int PageIndex = 1, int PageSize = 10)
        : IRequest<Result<PaginatedResult<TrackDto>>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public sealed class Handler(AppDbContext dbContext)
        : IRequestHandler<Request, Result<PaginatedResult<TrackDto>>>
    {
        public async Task<Result<PaginatedResult<TrackDto>>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var query = dbContext.Tracks.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(request.SearchTerm));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var tracks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TrackDto(t.Id, t.Name, t.CreatedAt, t.Skills.Count))
                .ToListAsync(cancellationToken);

            return new PaginatedResult<TrackDto>(
                tracks,
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
                    "/api/tracks",
                    async ([AsParameters] Request request, IMediator mediator) =>
                    {
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Tracks")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
