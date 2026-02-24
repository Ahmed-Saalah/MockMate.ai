using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Abstractions.Shared;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.Questions;

public sealed class UpdateMcqQuestion
{
    public sealed record Response(string Message);

    public record RequestDto(
        string Title,
        string Text,
        string SeniorityLevel,
        List<int> SkillIds,
        List<McqOptionDto> Options
    );

    public sealed record Request(int Id, RequestDto Data) : IRequest<Result<Response>>;

    public sealed record McqOptionDto(int? Id, string OptionText, bool IsCorrect);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Data.Text).NotEmpty();
            RuleFor(x => x.Data.SeniorityLevel).NotEmpty();
            RuleFor(x => x.Data.SkillIds).NotEmpty();

            RuleFor(x => x.Data.Options)
                .NotNull()
                .WithMessage("Options cannot be null.")
                .Must(o => o.Count >= 2)
                .WithMessage("At least 2 options required.")
                .Must(o => o.Count <= 4)
                .WithMessage("A maximum of 4 options is allowed.")
                .Must(o => o.Count(x => x.IsCorrect) == 1)
                .WithMessage("Exactly 1 option must be correct.");
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
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

            if (question is null || question.QuestionType != QuestionType.MCQ)
            {
                return new NotFound("MCQ Question not found.");
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

            var incomingOptionIds = request
                .Data.Options.Where(o => o.Id.HasValue && o.Id > 0)
                .Select(o => o.Id!.Value)
                .ToList();

            var optionsToRemove = question
                .Options.Where(o => !incomingOptionIds.Contains(o.Id))
                .ToList();

            foreach (var option in optionsToRemove)
            {
                question.Options.Remove(option);
            }

            foreach (var reqOption in request.Data.Options)
            {
                if (reqOption.Id.HasValue && reqOption.Id > 0)
                {
                    var existing = question.Options.FirstOrDefault(o => o.Id == reqOption.Id);
                    if (existing != null)
                    {
                        existing.OptionText = reqOption.OptionText;
                        existing.IsCorrect = reqOption.IsCorrect;
                    }
                }
                else
                {
                    question.Options.Add(
                        new McqOption
                        {
                            OptionText = reqOption.OptionText,
                            IsCorrect = reqOption.IsCorrect,
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
                    "/api/questions/mcq/{id:int}",
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
