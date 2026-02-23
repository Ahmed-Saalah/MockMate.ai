using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Tracks;

public sealed class GetTrackById
{
    public sealed record TrackDto(
        int Id,
        string Name,
        DateTime CreatedAt,
        int SkillCount
    );

    public sealed record Request(int Id)
        : IRequest<Result<TrackDto>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext dbContext, IValidator<Request> validator)
        : IRequestHandler<Request, Result<TrackDto>>
    {
        public async Task<Result<TrackDto>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            var track = await dbContext.Tracks
                .AsNoTracking()
                .Where(t => t.Id == request.Id)
                .Select(t => new TrackDto(
                    t.Id,
                    t.Name,
                    t.CreatedAt,
                    t.Skills.Count
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (track is null)
                return new NotFound();

            return track;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/tracks/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id));
                        return response.ToHttpResult();
                    })
                .WithTags("Tracks")
                .WithDescription("Get track by id")
                .RequireAuthorization(Roles.Admin);
        }
    }
}