using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Auth;

public sealed class ResetPassword
{
    public sealed record Request(string Email, string ResetToken, string NewPassword) : IRequest<Result<string>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Email).NotEmpty().EmailAddress();
            RuleFor(r => r.ResetToken).NotEmpty();
            RuleFor(r => r.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(100);
        }
    }

    public sealed class Handler(
        UserManager<User> userManager
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
                return new BadRequestError("Invalid request.");
            }

            var result = await userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new BadRequestError($"Password reset failed: {errors}");
            }

            return "Password has been successfully reset.";
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/users/reset-password",
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
