using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Helpers;

namespace MockMate.Api.Features.Skills;

public sealed class UpdateSkill
{
    public sealed record Response(int Id, string Name, List<int> TrackIds);

    public sealed record Request(int Id, UpdateSkillDto Data) : IRequest<Result<Response>>;

    public sealed record UpdateSkillDto(string Name, List<int> TrackIds);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Data.TrackIds).NotEmpty();
        }
    }

    public sealed class Handler(AppDbContext context) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var skill = await context
                .Skills.Include(s => s.Tracks)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (skill is null)
            {
                return new NotFoundError($"Skill with id {request.Id} was not found.");
            }

            var newNormalizedName = SkillNormalizer.Normalize(request.Data.Name);
            var duplicateExists = await context.Skills.AnyAsync(
                s => s.NormalizedName == newNormalizedName && s.Id != request.Id,
                cancellationToken
            );

            if (duplicateExists)
            {
                return new ConflictError("A skill with this name already exists.");
            }

            var tracks = await context
                .Tracks.Where(t => request.Data.TrackIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            if (tracks.Count != request.Data.TrackIds.Count)
            {
                return new NotFoundError("One or more tracks were not found.");
            }

            skill.Name = request.Data.Name;
            skill.NormalizedName = newNormalizedName;
            skill.Tracks.Clear();
            foreach (var track in tracks)
            {
                skill.Tracks.Add(track);
            }

            await context.SaveChangesAsync(cancellationToken);

            return new Response(skill.Id, skill.Name, skill.Tracks.Select(t => t.Id).ToList());
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "/api/skills/{id:int}",
                    async (int id, UpdateSkillDto data, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id, data));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Skills")
                .WithDescription("Updates a skill and its tracks")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
