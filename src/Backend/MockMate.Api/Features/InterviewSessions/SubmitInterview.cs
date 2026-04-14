using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Clients.AiService.Dtos;
using MockMate.Api.Clients.AiService.Interfaces;
using MockMate.Api.Common.Endpoints;
using MockMate.Api.Common.Errors;
using MockMate.Api.Common.Http;
using MockMate.Api.Common.Results;
using MockMate.Api.Constants;
using MockMate.Api.Data;
using MockMate.Api.Entities;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class SubmitInterview
{
    public sealed record Response(decimal Score, string? Feedback);

    public sealed record Request(int Id) : IRequest<Result<Response>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext context, IAiServiceClient aiServiceClient)
        : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var interview = await context
                .InterviewSessions.Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (interview is null)
                return new NotFoundError("Interview session not found");

            if (interview.EndDate != null)
                return new BadRequestError("Interview already submitted");

            var answers = interview.Answers ?? new List<SessionAnswer>();

            if (!answers.Any())
                return new BadRequestError("No answers found");

            var questionIds = answers.Select(x => x.QuestionId).Distinct().ToList();

            var questions = await context
                .Questions.Where(q => questionIds.Contains(q.Id))
                .ToListAsync(cancellationToken);

            var correctOptions = await context
                .McqOptions.Where(o => questionIds.Contains(o.QuestionId) && o.IsCorrect)
                .ToListAsync(cancellationToken);

            int totalScore = 0;
            int totalQuestions = answers.Count;

            var mcqAnswers = new List<McqAnswerDto>();
            var codingAnswers = new List<CodingAnswerDto>();

            foreach (var answer in answers)
            {
                var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);

                if (question is null)
                    continue;

                if (
                    question.QuestionType.Equals(
                        QuestionType.MCQ,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    var correct = correctOptions.FirstOrDefault(o =>
                        o.QuestionId == answer.QuestionId
                    );

                    var selectedOption = await context.McqOptions.FirstOrDefaultAsync(
                        o => o.Id == answer.SelectedOptionId,
                        cancellationToken
                    );

                    bool isCorrect = (correct != null && correct.Id == answer.SelectedOptionId);
                    if (isCorrect)
                        totalScore++;

                    mcqAnswers.Add(
                        new McqAnswerDto(
                            QuestionText: question.Text,
                            CandidateAnswer: selectedOption?.OptionText!,
                            IsCorrect: isCorrect
                        )
                    );
                }
                else if (
                    question.QuestionType.Equals(
                        QuestionType.Coding,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    bool isPassed = answer.IsCorrect;
                    if (isPassed)
                        totalScore++;

                    codingAnswers.Add(
                        new CodingAnswerDto(
                            QuestionTitle: question.Title,
                            QuestionText: question.Text,
                            Language: Judge0Languages.GetName(answer.LanguageId ?? 0),
                            SourceCode: answer.SubmittedCode,
                            Judge0Status: isPassed
                                ? ExecutionStatus.Passed
                                : ExecutionStatus.Failed,
                            Score: isPassed ? 100 : 0
                        )
                    );
                }
            }

            decimal percentage =
                totalQuestions == 0 ? 0 : Math.Round((decimal)totalScore / totalQuestions * 100, 2);

            interview.Score = (int)Math.Round(percentage);
            interview.EndDate = DateTime.UtcNow;

            var aiResponse = await aiServiceClient.GetInterviewFeedbackAsync(
                new FeedbackRequest(
                    InterviewId: interview.Id,
                    CodingAnswers: codingAnswers,
                    McqAnswers: mcqAnswers
                ),
                cancellationToken
            );

            interview.Feedback = aiResponse?.Feedback;
            await context.SaveChangesAsync(cancellationToken);

            return new Response(interview.Score, interview.Feedback);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/interviews/{id:int}/submit",
                    async (int id, IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request(id));
                        return result.ToHttpResult();
                    }
                )
                //.RequireAuthorization()
                .WithTags("Interviews");
        }
    }
}
