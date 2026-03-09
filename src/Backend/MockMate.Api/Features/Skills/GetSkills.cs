using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Skills;

public sealed class GetSkills
{
    public sealed record Response(int Id, string Name);

    public sealed record Request(
        int PageIndex = 1,
        int PageSize = 20,
        int? TrackId = null,
        string? SkillName = null
    ) : IRequest<Result<PaginatedResult<Response>>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PageIndex).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
            RuleFor(x => x.SkillName).MaximumLength(100);
        }
    }

    public sealed class Handler(AppDbContext context)
        : IRequestHandler<Request, Result<PaginatedResult<Response>>>
    {
        private readonly AppDbContext _context = context;

        public async Task<Result<PaginatedResult<Response>>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var query = _context.Skills.AsNoTracking().AsQueryable();

            if (request.TrackId.HasValue)
            {
                query = query.Where(s => s.Tracks.Any(t => t.Id == request.TrackId.Value));
            }

            if (!string.IsNullOrWhiteSpace(request.SkillName))
            {
                query = query.Where(s => s.Name.StartsWith(request.SkillName));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skills = await query
                .OrderBy(s => s.Name)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new Response(s.Id, s.Name))
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedResult<Response>(
                skills,
                totalCount,
                request.PageIndex,
                request.PageSize
            );

            return paginatedResult;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/skills",
                    async ([AsParameters] Request request, IMediator mediator) =>
                    {
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Skills")
                .RequireAuthorization();
        }
    }
}
