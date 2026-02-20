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
    public sealed record Response(int Id, string Name, int TrackId);

    public sealed record Request(string Name, int TrackId) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TrackId).GreaterThan(0).WithMessage("A valid TrackId is required.");
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
                return new ValidationError(validationResult.Errors);

            var trackExists = await dbContext.Tracks.AnyAsync(
                t => t.Id == request.TrackId,
                cancellationToken
            );
            if (!trackExists)
            {
                return new BadRequestError("The specified track does not exist.");
            }

            var skill = new Skill { Name = request.Name, TrackId = request.TrackId };
            dbContext.Skills.Add(skill);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(skill.Id, skill.Name, skill.TrackId);
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
