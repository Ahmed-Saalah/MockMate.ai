using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Entities;
using MockMate.Api.Extensions;

namespace MockMate.Api.Features.Users;

public sealed class UpdateUserProfile
{
    public sealed class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto value) => new() { Value = value };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed record ResponseDto(int UserId, string DisplayName, string PhoneNumber);

    public sealed record Request(string DisplayName, string PhoneNumber) : IRequest<Response>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^\+\d{1,3}\s\d+$")
                .WithMessage("Invalid phone number format. Example: +20 1012345678");
        }
    }

    public sealed class Handler(UserManager<User> userManager, IValidator<Request> validator)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            if (string.IsNullOrWhiteSpace(request.UserId))
                return new UnauthorizedError();

            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
                return new NotFound();

            user.DisplayName = request.DisplayName;
            user.PhoneNumber = request.PhoneNumber;

            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return new ValidationError(result.Errors);

            return new ResponseDto(
                user.Id,
                user.DisplayName ?? string.Empty,
                user.PhoneNumber ?? string.Empty
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "/api/users/profile",
                    async ([FromBody] Request request, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();

                        if (string.IsNullOrEmpty(userId))
                        {
                            return Results.Unauthorized();
                        }
                        request.UserId = userId;
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Users")
                .RequireAuthorization();
        }
    }
}
