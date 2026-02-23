using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Data;
namespace MockMate.Api.Features.Skills;

public sealed class GetAllSkills
{
    // =====================================
    // Query
    // =====================================
    public sealed class Query : IRequest<Response>
    {
        public int? TrackId { get; init; }
        public string? SkillName { get; init; }
    }

    // =====================================
    // Validator
    // =====================================
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SkillName)
                .MaximumLength(100);
        }
    }

    // =====================================
    // Response
    // =====================================
    public sealed class Response : Result<List<ResponseDto>>
    {
        public static implicit operator Response(List<ResponseDto> value)
            => new() { Value = value };

        public static implicit operator Response(DomainError error)
            => new() { Error = error };
    }

    // =====================================
    // Response DTO
    // =====================================
    public sealed class ResponseDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = default!;
    }

    // =====================================
    // Handler
    // =====================================
    public sealed class Handler : IRequestHandler<Query, Response>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _context.Skills
                .AsNoTracking()
                .AsQueryable();

            if (request.TrackId is not null)
            {
                query = query.Where(s =>
                    s.Tracks.Any(t => t.Id == request.TrackId));
            }

            if (!string.IsNullOrWhiteSpace(request.SkillName))
            {
                query = query.Where(s =>
                    s.Name.StartsWith(request.SkillName));
            }

            var skills = await query
                .Select(s => new ResponseDto
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToListAsync(cancellationToken);

            return skills;
        }
    }

    // =====================================
    // Endpoint
    // =====================================

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                "/api/skills",
                async (int? trackId, string? skillName, IMediator mediator) =>
                    {
                        var request = new GetAllSkills.Query
                        {
                            TrackId = trackId,
                            SkillName = skillName
                        };

                        var response = await mediator.Send(request);

                        return response.ToHttpResult();
                    }
                )
                .WithTags("Skills");
        }
    }
}