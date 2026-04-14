using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Data;
using MockMate.Api.Entities;
using System.Linq;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class SubmitInterview
{
    public sealed record Response(
        decimal Score,
        string? Feedback
    );

    public sealed record Request(int Id) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext context)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var interview = await context.InterviewSessions
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (interview is null)
                return new NotFoundError("Interview session not found");

            if (interview.EndDate != null)
                return new BadRequestError("Interview already submitted");
           
            var answers = interview.Answers ?? new List<SessionAnswer>();

            if (!answers.Any())
                return new BadRequestError("No answers found");

            var questionIds = answers
                .Select(x => x.QuestionId)
                .Distinct()
                .ToList();

            var questions = await context.Questions
                .Where(q => questionIds.Contains(q.Id))
                .ToListAsync(cancellationToken);

            var correctOptions = await context.McqOptions
                .Where(o => questionIds.Contains(o.QuestionId) && o.IsCorrect)
                .ToListAsync(cancellationToken);

            int totalScore = 0;
            int totalQuestions = answers.Count;

            foreach (var answer in answers)
            {
                var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);

                if (question is null)
                    continue;

                if (question.QuestionType == "mcq"  )
                {
                    var correct = correctOptions
                        .FirstOrDefault(o => o.QuestionId == answer.QuestionId);

                    if (correct != null && correct.Id == answer.SelectedOptionId)
                        totalScore++;
                }
            }

            decimal percentage = totalQuestions == 0
                ? 0
                : Math.Round((decimal)totalScore / totalQuestions * 100, 2);

            interview.Score = (int)Math.Round(percentage);
            interview.Feedback = null; 

            await context.SaveChangesAsync(cancellationToken);

            return new Response(interview.Score, interview.Feedback);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/interviews/{id:int}/submit",
                async (int id, IMediator mediator) =>
                {
                    var result = await mediator.Send(new Request(id));
                    return result.ToHttpResult();
                })
            .WithTags("Interviews");
        }
    }
}