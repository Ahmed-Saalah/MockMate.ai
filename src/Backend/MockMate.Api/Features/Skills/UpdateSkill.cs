using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Skills;

public sealed class UpdateSkill
{
    public sealed record Response(int Id, string Name, List<int> TrackIds );

    public sealed record Request(int Id, UpdateSkillDto Data)
        : IRequest<Result<Response>>;

    public sealed record UpdateSkillDto(
        string Name,
        List<int> TrackIds
    );

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Data.TrackIds)
                .NotEmpty();
        }
    }

    public sealed class Handler(AppDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var validationResult =
                await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            var skill = await context.Skills
                .Include(s => s.Tracks)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (skill is null)
                return new NotFound($"Skill with id {request.Id} was not found.");

            var tracks = await context.Tracks
                .Where(t => request.Data.TrackIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            if (tracks.Count != request.Data.TrackIds.Count)
                return new NotFound("One or more tracks were not found.");

            skill.Name = request.Data.Name;
            skill.Tracks.Clear();

            foreach (var track in tracks)
                skill.Tracks.Add(track);

            await context.SaveChangesAsync(cancellationToken);

            return new Response(skill.Id, skill.Name , skill.Tracks.Select(t => t.Id).ToList());
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                "/api/skills/{id:int}",
                async (int id, UpdateSkillDto body, IMediator mediator) =>
                {
                    var response = await mediator.Send(
                        new Request(id, body));

                    return response.ToHttpResult();
                })
                .WithTags("Skills")
                .WithDescription("Updates a skill and its tracks")
                .RequireAuthorization();
        }
    }
}