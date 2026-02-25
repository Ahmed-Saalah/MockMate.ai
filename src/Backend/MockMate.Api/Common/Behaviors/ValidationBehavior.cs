using FluentValidation;
using MediatR;
using MockMate.Api.Common.Errors;

namespace MockMate.Api.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            var validationError = new ValidationError(failures);

            var implicitCastMethod = typeof(TResponse).GetMethod(
                "op_Implicit",
                new[] { typeof(DomainError) }
            );

            if (implicitCastMethod != null)
            {
                return (TResponse)implicitCastMethod.Invoke(null, [validationError])!;
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
