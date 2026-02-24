using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Questions;

public sealed class DeleteQuestion
{
    public sealed record Response(string Message);

    public sealed record Request(int Id) : IRequest<Result<Response>>;

    public sealed class Handler(AppDbContext context) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var question = await context.Questions.FirstOrDefaultAsync(
                q => q.Id == request.Id,
                cancellationToken
            );

            if (question is null)
            {
                return new NotFound($"Question with ID {request.Id} was not found.");
            }

            context.Questions.Remove(question);
            await context.SaveChangesAsync(cancellationToken);

            return new Response("Question deleted successfully.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "/api/questions/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Questions")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
