using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Skills;

public sealed class UpdateSkill
{
    public sealed record Response(int Id, string Name);

    public sealed record Request(int Id, string Name)
        : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);
        }
    }

    public sealed class Handler(AppDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            var skill = await context.Skills
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (skill is null)
                return new NotFound($"Skill with id {request.Id} was not found.");

            skill.Name = request.Name;

            await context.SaveChangesAsync(cancellationToken);

            return new Response(skill.Id, skill.Name);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                "/api/skills/{id:int}",
                async (int id, Request body, IMediator mediator) =>
                {
                    var response = await mediator.Send(
                        new Request(id, body.Name));

                    return response.ToHttpResult();
                })
                .WithTags("Skills")
                .WithDescription("Updates a skill")
                .RequireAuthorization();
        }
    }
}