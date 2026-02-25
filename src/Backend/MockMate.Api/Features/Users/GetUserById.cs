using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Data;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Users;

public sealed class GetUserById
{
    public sealed class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto value) => new() { Value = value };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed record ResponseDto(
        int UserId,
        string DisplayName,
        string PhoneNumber,
        string AvatarPath,
        int InterveiwsCount,
        double AverageScore
    );

    public sealed record Request(int Id) : IRequest<Response>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);
            var response = await context
                .Users.Where(u => u.Id == request.Id)
                .Select(u => new ResponseDto(
                    u.Id,
                    u.DisplayName,
                    u.PhoneNumber,
                    u.AvatarPath,
                    u.InterviewSessions.Count(),
                    u.InterviewSessions.Select(i => (double?)i.Score).Average() ?? 0
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (response is null)
                return new NotFoundError();

            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/users/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var request = new Request(id);

                        var response = await mediator.Send(request);

                        return response.ToHttpResult();
                    }
                )
                .WithTags("Users")
                .RequireAuthorization();
        }
    }
}
