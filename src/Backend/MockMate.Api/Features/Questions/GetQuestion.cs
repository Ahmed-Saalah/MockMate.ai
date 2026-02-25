using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;

namespace MockMate.Api.Features.Questions;

public sealed class GetQuestion
{
    public sealed record Response(
        int Id,
        string Title,
        string Text,
        string SeniorityLevel,
        string QuestionType,
        DateTime CreatedAt,
        List<SkillDto> Skills,
        List<McqOptionDto> Options,
        List<LanguageTemplateDto> Templates,
        List<TestCaseDto> TestCases
    );

    public sealed record SkillDto(int Id, string Name);

    public sealed record McqOptionDto(int Id, string OptionText, bool IsCorrect);

    public sealed record TestCaseDto(int Id, string Input, string ExpectedOutput, bool IsHidden);

    public sealed record LanguageTemplateDto(
        int Id,
        int LanguageId,
        string LanguageName,
        decimal TimeLimit,
        int MemoryLimit,
        string DefaultCode,
        string DriverCode
    );

    public sealed record Request(int Id) : IRequest<Result<Response>>;

    public sealed class Handler(AppDbContext context) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var question = await context
                .Questions.AsNoTracking()
                .Include(q => q.Skills)
                .Include(q => q.Options)
                .Include(q => q.Templates)
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

            if (question is null)
            {
                return new NotFoundError($"Question with ID {request.Id} was not found.");
            }

            var response = new Response(
                question.Id,
                question.Title,
                question.Text,
                question.SeniorityLevel,
                question.QuestionType,
                question.CreatedAt,
                question.Skills.Select(s => new SkillDto(s.Id, s.Name)).ToList(),
                question
                    .Options.Select(o => new McqOptionDto(o.Id, o.OptionText, o.IsCorrect))
                    .ToList(),
                question
                    .Templates.Select(t => new LanguageTemplateDto(
                        t.Id,
                        t.LanguageId,
                        Judge0Languages.GetName(t.LanguageId),
                        t.TimeLimit,
                        t.MemoryLimit,
                        t.DefaultCode,
                        t.DriverCode
                    ))
                    .ToList(),
                question
                    .TestCases.Select(tc => new TestCaseDto(
                        tc.Id,
                        tc.Input,
                        tc.Output,
                        tc.IsHidden
                    ))
                    .ToList()
            );

            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/questions/{id:int}",
                    async (int id, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Questions")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
