using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Skills;

public sealed class DeleteSkill
{
    public sealed record Response(string Message);

    public sealed record Request(int Id) : IRequest<Result<Response>>;

    public sealed class Handler(AppDbContext context)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var skill = await context.Skills.FindAsync( request.Id  , cancellationToken);

            if (skill is null)
            {
                return new NotFound($"Skill with ID {request.Id} was not found.");
            }

            context.Skills.Remove(skill);
            await context.SaveChangesAsync(cancellationToken);

            return new Response("Skill deleted successfully.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "/api/skills/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Skills")
                .WithDescription("Deletes a skill by its ID.")
                .RequireAuthorization(Roles.Admin);
        }
    }
}