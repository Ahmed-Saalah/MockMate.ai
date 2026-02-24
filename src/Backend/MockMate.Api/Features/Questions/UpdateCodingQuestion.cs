using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Questions;

public sealed class UpdateCodingQuestion
{
    public sealed record Response(string Message);

    public sealed record Request(int Id, RequestDto Data) : IRequest<Result<Response>>;

    public sealed record RequestDto(
        string Title,
        string Text,
        string SeniorityLevel,
        List<int> SkillIds,
        List<LanguageTemplateDto> Templates,
        List<TestCaseDto> TestCases
    );

    public sealed record LanguageTemplateDto(
        int? Id,
        int LanguageId,
        decimal TimeLimit,
        int MemoryLimit,
        string DefaultCode,
        string DriverCode
    );

    public sealed record TestCaseDto(int? Id, string Input, string ExpectedOutput, bool IsHidden);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Data.Text).NotEmpty();
            RuleFor(x => x.Data.SkillIds).NotEmpty();

            RuleFor(x => x.Data.Templates)
                .NotEmpty()
                .Must(t => t != null && t.Select(x => x.LanguageId).Distinct().Count() == t.Count)
                .WithMessage("Cannot have duplicate languages.");

            RuleFor(x => x.Data.TestCases).NotEmpty();
        }
    }

    public sealed class Handler(AppDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ValidationError(validationResult.Errors);
            }

            var question = await context
                .Questions.Include(q => q.Skills)
                .Include(q => q.Templates)
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

            if (question is null || question.QuestionType != QuestionType.Coding)
            {
                return new NotFound("Coding Question not found.");
            }

            question.Title = request.Data.Title;
            question.Text = request.Data.Text;
            question.SeniorityLevel = request.Data.SeniorityLevel;

            var newSkills = await context
                .Skills.Where(s => request.Data.SkillIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            question.Skills.Clear();
            foreach (var skill in newSkills)
            {
                question.Skills.Add(skill);
            }

            var incomingTemplateIds = request
                .Data.Templates.Where(t => t.Id.HasValue && t.Id > 0)
                .Select(t => t.Id!.Value)
                .ToList();

            var templatesToRemove = question
                .Templates.Where(t => !incomingTemplateIds.Contains(t.Id))
                .ToList();

            foreach (var t in templatesToRemove)
            {
                question.Templates.Remove(t);
            }

            foreach (var reqTemp in request.Data.Templates)
            {
                if (!Judge0Languages.Supported.ContainsKey(reqTemp.LanguageId))
                {
                    return new BadRequestError($"Unsupported language ID: {reqTemp.LanguageId}");
                }

                if (reqTemp.Id.HasValue && reqTemp.Id > 0)
                {
                    var existing = question.Templates.FirstOrDefault(t => t.Id == reqTemp.Id);
                    if (existing != null)
                    {
                        existing.LanguageId = reqTemp.LanguageId;
                        existing.TimeLimit = reqTemp.TimeLimit;
                        existing.MemoryLimit = reqTemp.MemoryLimit;
                        existing.DefaultCode = reqTemp.DefaultCode;
                        existing.DriverCode = reqTemp.DriverCode;
                    }
                }
                else
                {
                    question.Templates.Add(
                        new LanguageTemplate
                        {
                            LanguageId = reqTemp.LanguageId,
                            TimeLimit = reqTemp.TimeLimit,
                            MemoryLimit = reqTemp.MemoryLimit,
                            DefaultCode = reqTemp.DefaultCode,
                            DriverCode = reqTemp.DriverCode,
                        }
                    );
                }
            }

            var incomingTestCaseIds = request
                .Data.TestCases.Where(t => t.Id.HasValue && t.Id > 0)
                .Select(t => t.Id!.Value)
                .ToList();

            var testCasesToRemove = question
                .TestCases.Where(t => !incomingTestCaseIds.Contains(t.Id))
                .ToList();

            foreach (var tc in testCasesToRemove)
            {
                question.TestCases.Remove(tc);
            }

            foreach (var reqTc in request.Data.TestCases)
            {
                if (reqTc.Id.HasValue && reqTc.Id > 0)
                {
                    var existing = question.TestCases.FirstOrDefault(t => t.Id == reqTc.Id);
                    if (existing != null)
                    {
                        existing.Input = reqTc.Input;
                        existing.Output = reqTc.ExpectedOutput;
                        existing.IsHidden = reqTc.IsHidden;
                    }
                }
                else
                {
                    question.TestCases.Add(
                        new TestCase
                        {
                            Input = reqTc.Input,
                            Output = reqTc.ExpectedOutput,
                            IsHidden = reqTc.IsHidden,
                        }
                    );
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            return new Response("Question updated successfully.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "/api/questions/coding/{id:int}",
                    async (int id, [FromBody] RequestDto data, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(id, data));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Questions")
                .RequireAuthorization(Roles.Admin);
        }
    }
}
