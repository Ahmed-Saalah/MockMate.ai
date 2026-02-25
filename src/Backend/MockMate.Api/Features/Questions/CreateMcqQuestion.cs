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

public sealed class CreateMcqQuestion
{
    public sealed record Response(
        int Id,
        string Title,
        string Text,
        string SeniorityLevel,
        string QuestionType,
        DateTime CreatedAt,
        List<SkillDto> Skills,
        List<McqOptionResponseDto> Options
    );

    public sealed record SkillDto(int Id, string Name);

    public sealed record McqOptionResponseDto(int Id, string OptionText, bool IsCorrect);

    public sealed record Request(
        string Title,
        string Text,
        string SeniorityLevel,
        List<int> SkillIds,
        List<McqOptionDto> Options
    ) : IRequest<Result<Response>>;

    public sealed record McqOptionDto(string OptionText, bool IsCorrect);

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

            RuleFor(x => x.Options)
                .NotEmpty()
                .Must(x => x != null && x.Count >= 2)
                .WithMessage("An MCQ must have at least 2 options.")
                .Must(x => x != null && x.Count(o => o.IsCorrect) == 1)
                .WithMessage("An MCQ must have exactly 1 correct option.");
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
                QuestionType = QuestionType.MCQ,
                Skills = skills,
                Options = request
                    .Options.Select(o => new McqOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
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
                    .Options.Select(o => new McqOptionResponseDto(o.Id, o.OptionText, o.IsCorrect))
                    .ToList()
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/api/questions/mcq",
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
