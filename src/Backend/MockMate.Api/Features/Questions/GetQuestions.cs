using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Questions;

public sealed class GetQuestions
{
    public sealed record QuestionSummaryDto(
        int Id,
        string Title,
        string SeniorityLevel,
        string QuestionType,
        DateTime CreatedAt,
        List<string> Skills
    );

    public sealed record Request(
        int PageIndex = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        string? QuestionType = null,
        string? SeniorityLevel = null,
        int? SkillId = null
    ) : IRequest<Result<PaginatedResult<QuestionSummaryDto>>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PageIndex).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        }
    }

    public sealed class Handler(AppDbContext context)
        : IRequestHandler<Request, Result<PaginatedResult<QuestionSummaryDto>>>
    {
        public async Task<Result<PaginatedResult<QuestionSummaryDto>>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {

            var query = context.Questions.AsNoTracking().Include(q => q.Skills).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(q => q.Title.Contains(request.SearchTerm));

            if (!string.IsNullOrWhiteSpace(request.QuestionType))
                query = query.Where(q => q.QuestionType == request.QuestionType);

            if (!string.IsNullOrWhiteSpace(request.SeniorityLevel))
                query = query.Where(q => q.SeniorityLevel == request.SeniorityLevel);

            if (request.SkillId.HasValue)
                query = query.Where(q => q.Skills.Any(s => s.Id == request.SkillId.Value));

            var totalCount = await query.CountAsync(cancellationToken);

            var questions = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = questions
                .Select(q => new QuestionSummaryDto(
                    q.Id,
                    q.Title,
                    q.SeniorityLevel,
                    q.QuestionType,
                    q.CreatedAt,
                    q.Skills.Select(s => s.Name).ToList()
                ))
                .ToList();

            var paginatedResult = new PaginatedResult<QuestionSummaryDto>(
                items,
                totalCount,
                request.PageIndex,
                request.PageSize
            );

            return paginatedResult;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/questions",
                    async ([AsParameters] Request request, IMediator mediator) =>
                    {
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Questions")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
