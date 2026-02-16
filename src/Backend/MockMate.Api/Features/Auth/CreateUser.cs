using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Entities;
using MockMate.Api.Services.JwtService;
using PhoneNumbers;

namespace MockMate.Api.Features.Auth;

public sealed class CreateUser
{
    public sealed class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto successResult) =>
            new() { Value = successResult };

        public static implicit operator Response(DomainError errorResult) =>
            new() { Error = errorResult };
    }

    public sealed record ResponseDto(int UserId, string AccessToken, string RefreshToken);

    public sealed record Request(
        string Username,
        string Email,
        string PhoneNumber,
        string Password,
        string DisplayName,
        string Role,
        string? AvatarPath
    ) : IRequest<Response>;

    public sealed class Validator : AbstractValidator<Request>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public Validator(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username is required")
                .MinimumLength(3)
                .WithMessage("Username too short")
                .MustAsync(
                    async (username, ct) => await _userManager.FindByNameAsync(username) is null
                )
                .WithMessage("Username already taken");

            RuleFor(r => r.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Invalid email format")
                .MustAsync(async (email, ct) => await _userManager.FindByEmailAsync(email) is null)
                .WithMessage("Email already registered");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password length must be greater than 8");

            RuleFor(r => r.Role)
                .NotEmpty()
                .MustAsync(async (role, ct) => await _roleManager.RoleExistsAsync(role))
                .WithMessage(r => $"Role '{r.Role}' does not exist");

            RuleFor(r => r.DisplayName).NotEmpty().MaximumLength(100);

            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required");

            When(
                r => !string.IsNullOrEmpty(r.PhoneNumber),
                () =>
                {
                    RuleFor(r => r.PhoneNumber)
                        .Matches(@"^\+\d{1,3}\s\d+$")
                        .WithMessage("Invalid phone number format")
                        .Must(phoneNumber =>
                        {
                            PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();

                            try
                            {
                                PhoneNumber numberProto = phoneNumberUtil.Parse(phoneNumber, null);
                                return phoneNumberUtil.IsValidNumber(numberProto);
                            }
                            catch (NumberParseException)
                            {
                                return false;
                            }
                        })
                        .WithMessage("Phone Number not valid");
                }
            );
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        ITokenService tokenService,
        IValidator<Request> validator,
        IHttpContextAccessor httpContextAccessor
    ) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return new ValidationError(validationResult.Errors);
            }

            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DisplayName = request.DisplayName,
                AvatarPath = request.AvatarPath ?? "/images/default-avatar.png",
                RegisteredAt = DateTime.UtcNow,
            };

            var identityResult = await userManager.CreateAsync(user, request.Password);

            if (!identityResult.Succeeded)
            {
                return new ValidationError(identityResult.Errors);
            }

            await userManager.AddToRoleAsync(user, request.Role);
            var accessToken = tokenService.GenerateAccessToken(user, [request.Role], []);

            var refreshToken = tokenService.GenerateRefreshToken(
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                    ?? "unknown"
            );

            user.RefreshTokens.Add(refreshToken);
            await userManager.UpdateAsync(user);

            return new ResponseDto(user.Id, accessToken, refreshToken.Token);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/api/users",
                    async ([FromBody] Request request, IMediator mediator) =>
                    {
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Users")
                .AllowAnonymous();
        }
    }
}
