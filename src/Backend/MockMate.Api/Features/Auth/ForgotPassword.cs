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
using MockMate.Api.Services.EmailService;

namespace MockMate.Api.Features.Auth;

public sealed class ForgotPassword
{
    public sealed record Request(string Email) : IRequest<Result<string>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        IMemoryCache memoryCache,
        IEmailSender emailSender
    ) : IRequestHandler<Request, Result<string>>
    {
        public async Task<Result<string>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                // To prevent email enumeration attacks, always return success even if user not found.
                return "If your email is registered, you will receive an OTP shortly.";
            }

            // Generate 5-digit OTP
            var otp = new Random().Next(10000, 99999).ToString();

            // Store in cache for 5 minutes
            memoryCache.Set($"OTP_{user.Email}", otp, TimeSpan.FromMinutes(5));

            // Send Email
            var message = $@"
                <h2>Reset Your Password</h2>
                <p>Use the following OTP to reset your MockMate password. It will expire in 5 minutes.</p>
                <h3 style='background:#f4f4f4;padding:10px;display:inline-block;letter-spacing:2px;'>{otp}</h3>
                <p>If you did not request a password reset, please ignore this email.</p>
            ";

            await emailSender.SendEmailAsync(user.Email!, "MockMate - Password Reset OTP", message);

            return "If your email is registered, you will receive an OTP shortly.";
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/users/forgot-password",
                    async ([FromBody] Request request, IMediator mediator) =>
                    {
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Users")
                .RequireRateLimiting("ForgotPasswordLimiter")
                .AllowAnonymous();
        }
    }
}
