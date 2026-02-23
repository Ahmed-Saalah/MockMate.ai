using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Tracks;

public sealed class UpdateTrack
{
    public sealed record Response(int Id, string Name, DateTime CreatedAt);

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

            var normalizedName = request.Name.Trim();

            var nameExists = await dbContext.Tracks
                .AnyAsync(t => t.Name == normalizedName && t.Id != request.Id, cancellationToken);

            if (nameExists)
                return new ConflictError("A track with this name already exists.");

            track.Name = normalizedName;

            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(track.Id, track.Name, track.CreatedAt);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "/api/tracks/{id:int}",
                    async (int id, string name, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id, name));
                        return response.ToHttpResult();
                    })
                .WithTags("Tracks")
                .WithDescription("Updates track name")
                .RequireAuthorization(Roles.Admin);
        }
    }
}