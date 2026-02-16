using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Data;
using MockMate.Api.Entities;
using MockMate.Api.Services.JwtService;

namespace Auth.API.Features;

public sealed class RefreshToken
{
    public sealed record Response(string AccessToken, string RefreshToken);

    public sealed record Request(string Token) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage("Refresh token is required.");
        }
    }

    public sealed class Handler(
        AppDbContext dbContext,
        UserManager<User> userManager,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor
    ) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var existingToken = await dbContext
                .RefreshTokens.Include(rt => rt.User)
                .SingleOrDefaultAsync(rt => rt.Token == request.Token, cancellationToken);

            if (existingToken is null)
            {
                return new UnauthorizedError("Invalid refresh token.");
            }

            if (!existingToken.IsActive)
            {
                return new UnauthorizedError("Token is expired or already revoked.");
            }

            var ipAddress =
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                ?? "unknown";

            var user = existingToken.User!;
            var roles = await userManager.GetRolesAsync(user);
            var userClaims = await userManager.GetClaimsAsync(user);
            var newAccessToken = tokenService.GenerateAccessToken(user, roles, userClaims);
            var newRefreshToken = tokenService.GenerateRefreshToken(ipAddress);

            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.ReplacedByToken = newRefreshToken.Token;

            user.RefreshTokens.Add(newRefreshToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(newAccessToken, newRefreshToken.Token);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/users/refresh",
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