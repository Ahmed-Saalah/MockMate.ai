using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Auth;

public sealed class Logout
{
    public sealed record Response(string Message);

    public sealed record Request(string RefreshToken) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.RefreshToken).NotEmpty().WithMessage("RefreshToken is required.");
        }
    }

    public sealed class Handler(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var user = await dbContext
                .Users.Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(
                    u => u.RefreshTokens.Any(t => t.Token == request.RefreshToken),
                    cancellationToken
                );

            if (user is null)
            {
                return new Response("Logged out successfully.");
            }

            var token = user.RefreshTokens.Single(t => t.Token == request.RefreshToken);
            if (token.RevokedAt != null)
            {
                return new Response("Token was already revoked.");
            }

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp =
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                ?? "unknown";

            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response("Logged out successfully.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/users/logout",
                    async ([FromBody] Request request, IMediator mediator) =>
                    {
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Users")
                .AllowAnonymous();
        }
    }
}
