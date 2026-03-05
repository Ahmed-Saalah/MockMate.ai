using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Results;
using MockMate.Api.Common.Errors;
using MockMate.Api.Data;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Http;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class GetInterviewSessionById
{
    public sealed record Response(
        int InterviewSessionId,
        int UserId,
        string UserName,
        decimal Score,
        DateTime StartDate,
        DateTime? EndDate,
        string? Feedback
    );

    public sealed record Request(int Id)
        : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new ValidationError(validationResult.Errors);

            var session = await context.InterviewSessions
                .AsNoTracking()
                .Where(s => s.Id == request.Id)
                .Select(s => new Response(
                    s.Id,
                    s.UserId,
                    s.User.UserName ?? string.Empty,
                    s.Score,
                    s.StartDate,
                    s.EndDate,
                    s.Feedback
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (session is null)
                return new NotFoundError();

            return session;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/interview-sessions/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id));
                        return response.ToHttpResult();
                    })
                .WithTags("Interviews")
                .RequireAuthorization();
        }
    }
}