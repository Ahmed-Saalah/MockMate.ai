using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Entities;
using MockMate.Api.Extensions;
using MockMate.Api.Services.StorageService;

namespace MockMate.Api.Features.Users;

public sealed class UpdateProfile
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
        string AvatarPath
    );

    public sealed record Request(string? DisplayName, string? PhoneNumber, IFormFile? Image)
        : IRequest<Response>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x)
                .Must(x =>
                    x.DisplayName is not null || x.PhoneNumber is not null || x.Image is not null
                )
                .WithMessage("At least one field must be provided.");

            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(100)
                .When(x => x.DisplayName is not null);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^\+\d{1,3}\s\d+$")
                .WithMessage("Invalid phone number format. Example: +20 1012345678")
                .When(x => x.PhoneNumber is not null);
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        IImageStorageService imageStorageService
    ) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return new UnauthorizedError();

            var user = await userManager.FindByIdAsync(request.UserId);

            if (user is null)
                return new NotFoundError();

            if (request.DisplayName is not null)
                user.DisplayName = request.DisplayName;

            if (request.PhoneNumber is not null)
                user.PhoneNumber = request.PhoneNumber;

            if (request.Image is not null && request.Image.Length > 0)
            {
                var newAvatarUrl = await imageStorageService.UploadImageAsync(
                    request.Image,
                    cancellationToken: cancellationToken
                );

                user.AvatarPath = newAvatarUrl;
            }

            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return new ValidationError(result.Errors);

            return new ResponseDto(
                user.Id,
                user.DisplayName ?? string.Empty,
                user.PhoneNumber ?? string.Empty,
                user.AvatarPath ?? string.Empty
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "/api/users/profile",
                    async (
                        [FromForm] string? displayName,
                        [FromForm] string? phoneNumber,
                        IFormFile? image,
                        IMediator mediator,
                        ClaimsPrincipal user
                    ) =>
                    {
                        var userId = user.GetUserId();
                        if (string.IsNullOrEmpty(userId))
                            return Results.Unauthorized();

                        var request = new Request(displayName, phoneNumber, image)
                        {
                            UserId = userId,
                        };

                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Users")
                .RequireAuthorization()
                .DisableAntiforgery();
        }
    }
}
