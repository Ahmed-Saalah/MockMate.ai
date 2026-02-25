using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Skills;

public sealed class GetSkill
{
    public sealed record ResponseDto(int Id, string Name, List<TrackDto> Tracks);

    public sealed record TrackDto(int Id, string Name);

    public sealed record Request(int Id) : IRequest<Result<ResponseDto>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Result<ResponseDto>>
    {
        public async Task<Result<ResponseDto>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            var skill = await context
                .Skills.AsNoTracking()
                .Where(s => s.Id == request.Id)
                .Select(s => new ResponseDto(
                    s.Id,
                    s.Name,
                    s.Tracks.Select(t => new TrackDto(t.Id, t.Name)).ToList()
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (skill is null)
                return new NotFoundError($"Skill with id {request.Id} was not found.");

            return skill;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/skills/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Skills");
        }
    }
}
