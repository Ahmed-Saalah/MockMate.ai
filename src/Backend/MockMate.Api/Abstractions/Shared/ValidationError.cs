using System.Net;
using Microsoft.AspNetCore.Identity;

namespace MockMate.Api.Abstractions.Shared;

[HttpCode(HttpStatusCode.BadRequest)]
public class ValidationError : DomainError
{
    public override string Code => "validation_error";
    public override string Message => "One or more validation errors occurred.";
    public Dictionary<string, string[]> ValidationErrors { get; set; }

    public ValidationError(List<FluentValidation.Results.ValidationFailure> validationFailures)
    {
        ValidationErrors = validationFailures
            .GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
    }

    public ValidationError(IEnumerable<IdentityError> errors)
    {
        ValidationErrors = errors
            .GroupBy(x => x.Code)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Description).ToArray());
    }
}
