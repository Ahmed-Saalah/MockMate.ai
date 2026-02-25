using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Tracks;

public sealed class CreateTrack
{
    public sealed record Response(int Id, string Name, DateTime CreatedAt);

    public sealed record Request(string Name) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Track name is required.")
                .MaximumLength(100)
                .WithMessage("Track name must not exceed 100 characters.");
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

            if (await dbContext.Tracks.AnyAsync(t => t.Name == request.Name, cancellationToken))
            {
                return new ConflictError("A track with this name already exists.");
            }

            var track = new Track { Name = request.Name };

            dbContext.Tracks.Add(track);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(track.Id, track.Name, track.CreatedAt);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/api/tracks",
                    async ([FromBody] Request request, IMediator mediator) =>
                    {
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Tracks")
                .WithDescription("Creates a new track")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
