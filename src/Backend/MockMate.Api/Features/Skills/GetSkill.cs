using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Skills
{
    public sealed class GetSkill
    {
        public sealed record Request(int Id) : IRequest<Response>;
        public sealed class validator : AbstractValidator<Request>
        {
            public validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
            }
        }

        public sealed class Response : Result<ResponseDto>
        {
            public static implicit operator Response(ResponseDto value)
                => new() { Value = value };

            public static implicit operator Response(DomainError error)
                => new() { Error = error };
        }
        public sealed record ResponseDto
        (
            int Id,
            string Name,
            List<TrackDto> Tracks
        );
        public sealed record TrackDto
        (
           int Id,
           string Name
        );

        public sealed class Handler : IRequestHandler<Request, Response>
        {
            private readonly AppDbContext _context;

            public Handler(AppDbContext context)
            {
                _context = context;
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var skill = await _context.Skills
                    .AsNoTracking()
                    .Where(s => s.Id == request.Id)
                    .Select(s => new ResponseDto(
                        s.Id,
                        s.Name,
                        s.Tracks
                            .Select(t => new TrackDto(t.Id, t.Name))
                            .ToList()
                    ))
                    .FirstOrDefaultAsync(cancellationToken);

                if (skill is null)
                    return new NotFound($"Skill with id {request.Id} was not found.");
                return skill;
            }
            public sealed class Endpoint : IEndpoint
            {
                public void Map(IEndpointRouteBuilder app)
                {
                    app.MapGet(
                            "/api/skills/{id:int}",
                            async (int id, IMediator mediator) =>
                            {
                                var request = new Request(id);

                                var response = await mediator.Send(request);

                                return response.ToHttpResult();
                            }
                        )
                        .WithTags("Skills");
                }
            }
        }
    }
}
