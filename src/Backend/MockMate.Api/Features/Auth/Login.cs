using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;
using MockMate.Api.Services.JwtService;

namespace MockMate.Api.Features.Auth;

public sealed class Login
{
    public sealed record Response(
        string AccessToken,
        string RefreshToken,
        string Role,
        ProfileData Profile
    );

    public sealed record ProfileData(
        string UserName,
        string Email,
        string DisplayName,
        string? AvatarPath
    );

    public sealed record Request(string Email, string Password) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .MaximumLength(50)
                .WithMessage("Email must not exceed 50 characters.");

            RuleFor(r => r.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(100)
                .WithMessage("Password must not exceed 100 characters.");
        }
    }

    public sealed class Handler(
        AppDbContext dbContext,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager,
        IValidator<Request> validator
    ) : IRequestHandler<Request, Result<Response>>
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

            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return new UnauthorizedError("Invalid email or password.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? Roles.User;
            var userClaims = await userManager.GetClaimsAsync(user);

            var accessToken = tokenService.GenerateAccessToken(user, roles, userClaims);

            var ipAddress =
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                ?? "unknown";

            var refreshToken = tokenService.GenerateRefreshToken(ipAddress);

            user.RefreshTokens.Add(refreshToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            var profile = new ProfileData(
                user.UserName!,
                user.Email!,
                user.DisplayName ?? user.UserName!,
                user.AvatarPath
            );

            return new Response(accessToken, refreshToken.Token, primaryRole, profile);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/users/login",
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
