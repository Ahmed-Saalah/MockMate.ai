using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Questions;

public sealed class CreateCodingQuestion
{
    public sealed record Response(
        int Id,
        string Title,
        string Text,
        string SeniorityLevel,
        string QuestionType,
        DateTime CreatedAt,
        List<SkillDto> Skills,
        List<LanguageTemplateResponseDto> Templates,
        List<TestCaseResponseDto> TestCases
    );

    public sealed record SkillDto(int Id, string Name);

    public sealed record TestCaseResponseDto(
        int Id,
        string Input,
        string ExpectedOutput,
        bool IsHidden
    );

    public sealed record LanguageTemplateResponseDto(
        int Id,
        int LanguageId,
        string LanguageName,
        decimal TimeLimit,
        int MemoryLimit,
        string DefaultCode,
        string DriverCode
    );

    public sealed record Request(
        string Title,
        string Text,
        string SeniorityLevel,
        List<int> SkillIds,
        List<LanguageTemplateDto> Templates,
        List<TestCaseDto> TestCases
    ) : IRequest<Result<Response>>;

    public sealed record TestCaseDto(string Input, string ExpectedOutput, bool IsHidden);

    public sealed record LanguageTemplateDto(
        int LanguageId,
        decimal TimeLimit,
        int MemoryLimit,
        string DefaultCode,
        string DriverCode
    );

    public sealed class TestCaseDtoValidator : AbstractValidator<TestCaseDto>
    {
        public TestCaseDtoValidator()
        {
            RuleFor(x => x.Input).NotEmpty();
            RuleFor(x => x.ExpectedOutput).NotEmpty();
        }
    }

    public sealed class LanguageTemplateDtoValidator : AbstractValidator<LanguageTemplateDto>
    {
        public LanguageTemplateDtoValidator()
        {
            RuleFor(x => x.LanguageId)
                .Must(id => Judge0Languages.Supported.ContainsKey(id))
                .WithMessage("Unsupported Language ID.");

            RuleFor(x => x.TimeLimit).GreaterThan(0).LessThanOrEqualTo(10);
            RuleFor(x => x.MemoryLimit).GreaterThan(0).LessThanOrEqualTo(512000);
            RuleFor(x => x.DefaultCode).NotEmpty();
            RuleFor(x => x.DriverCode).NotEmpty();
        }
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Text).NotEmpty();
            RuleFor(x => x.SeniorityLevel).NotEmpty();

            RuleFor(x => x.SkillIds)
                .NotEmpty()
                .WithMessage("At least one SkillId must be provided.")
                .Must(x => x != null && x.All(id => id > 0))
                .WithMessage("All SkillIds must be valid positive integers.");

            RuleFor(x => x.TestCases)
                .NotEmpty()
                .WithMessage("A coding question must have at least one test case.");
            RuleForEach(x => x.TestCases).SetValidator(new TestCaseDtoValidator());

            RuleFor(x => x.Templates)
                .NotEmpty()
                .WithMessage("At least one language template must be provided.")
                .Must(t => t != null && t.Select(x => x.LanguageId).Distinct().Count() == t.Count)
                .WithMessage("Cannot have duplicate language templates for the same question.");

            RuleForEach(x => x.Templates).SetValidator(new LanguageTemplateDtoValidator());
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

            var uniqueSkillIds = request.SkillIds.Distinct().ToList();
            var skills = await context
                .Skills.Where(s => uniqueSkillIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            if (skills.Count != uniqueSkillIds.Count)
            {
                return new BadRequestError("One or more specified skills do not exist.");
            }

            var question = new Question
            {
                Title = request.Title,
                Text = request.Text,
                SeniorityLevel = request.SeniorityLevel,
                QuestionType = QuestionType.Coding,
                Skills = skills,

                Templates = request
                    .Templates.Select(t => new LanguageTemplate
                    {
                        LanguageId = t.LanguageId,
                        TimeLimit = t.TimeLimit,
                        MemoryLimit = t.MemoryLimit,
                        DefaultCode = t.DefaultCode,
                        DriverCode = t.DriverCode,
                    })
                    .ToList(),

                TestCases = request
                    .TestCases.Select(tc => new TestCase
                    {
                        Input = tc.Input,
                        Output = tc.ExpectedOutput,
                        IsHidden = tc.IsHidden,
                    })
                    .ToList(),
            };

            context.Questions.Add(question);
            await context.SaveChangesAsync(cancellationToken);

            return new Response(
                question.Id,
                question.Title,
                question.Text,
                question.SeniorityLevel,
                question.QuestionType,
                question.CreatedAt,
                skills.Select(s => new SkillDto(s.Id, s.Name)).ToList(),
                question
                    .Templates.Select(t => new LanguageTemplateResponseDto(
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
                    .TestCases.Select(tc => new TestCaseResponseDto(
                        tc.Id,
                        tc.Input,
                        tc.Output,
                        tc.IsHidden
                    ))
                    .ToList()
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/api/questions/coding",
                    async ([FromBody] Request request, IMediator mediator) =>
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
