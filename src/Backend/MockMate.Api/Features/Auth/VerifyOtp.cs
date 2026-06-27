using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Auth;

public sealed class VerifyOtp
{
    public sealed record Response(string ResetToken);

    public sealed record Request(string Email, string Otp) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Email).NotEmpty().EmailAddress();
            RuleFor(r => r.Otp).NotEmpty().Length(5).WithMessage("OTP must be exactly 5 digits.");
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        IMemoryCache memoryCache
    ) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return new BadRequestError("Invalid email or OTP.");
            }

            var cacheKey = $"OTP_{user.Email}";
            if (!memoryCache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.Otp)
            {
                return new BadRequestError("Invalid or expired OTP.");
            }

            // OTP verified, remove from cache to prevent reuse
            memoryCache.Remove(cacheKey);

            // Generate official identity reset token
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

            return new Response(resetToken);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/users/verify-otp",
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
