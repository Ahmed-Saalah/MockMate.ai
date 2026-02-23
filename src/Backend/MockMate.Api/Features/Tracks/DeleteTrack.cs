using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Tracks;

public sealed class DeleteTrack
{
    public sealed record Response(int Id, string Name);

    public sealed record Request(int Id)
        : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext dbContext, IValidator<Request> validator)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            var track = await dbContext.Tracks
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (track is null)
                return new NotFound();

            dbContext.Tracks.Remove(track);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(track.Id, track.Name);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "/api/tracks/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id));
                        return response.ToHttpResult();
                    })
                .WithTags("Tracks")
                .WithDescription("Deletes a track")
                .RequireAuthorization(Roles.Admin);
        }
    }
}