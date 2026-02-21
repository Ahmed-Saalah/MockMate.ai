using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Skills;

public sealed class CreateSkill
{
    public sealed record Response(int Id, string Name, List<int> TrackIds);

    public sealed record Request(string Name, List<int> TrackIds) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

            RuleFor(x => x.TrackIds)
                .NotEmpty()
                .WithMessage("At least one TrackId must be provided.")
                .Must(x => x != null && x.All(id => id > 0))
                .WithMessage("All TrackIds must be valid positive integers.");
        }
    }

    public sealed class Handler(AppDbContext dbContext, IValidator<Request> validator)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ValidationError(validationResult.Errors);
            }

            var skillExists = await dbContext.Skills.AnyAsync(
                s => s.Name.ToLower() == request.Name.ToLower(),
                cancellationToken
            );
            if (skillExists)
            {
                return new BadRequestError("A skill with this name already exists.");
            }

            var uniqueTrackIds = request.TrackIds.Distinct().ToList();
            var tracks = await dbContext
                .Tracks.Where(t => uniqueTrackIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            if (tracks.Count != uniqueTrackIds.Count)
            {
                return new BadRequestError("One or more specified tracks do not exist.");
            }

            var skill = new Skill { Name = request.Name, Tracks = tracks };
            dbContext.Skills.Add(skill);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(skill.Id, skill.Name, tracks.Select(t => t.Id).ToList());
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/api/skills",
                    async ([FromBody] Request request, IMediator mediator) =>
                    {
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Skills")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
