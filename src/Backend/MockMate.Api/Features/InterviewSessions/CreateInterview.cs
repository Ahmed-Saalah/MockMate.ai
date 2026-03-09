using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
using MockMate.Api.Extensions;
using MockMate.Api.Helpers;

namespace MockMate.Api.Features.InterviewSessions;

public sealed class CreateInterview
{
    public sealed record Request(IFormFile CvFile, string? JobDescription)
        : IRequest<Result<Response>>
    {
        [JsonIgnore]
        public int UserId { get; set; }
    }

    public sealed record Response(
        int InterviewSessionId,
        List<McqQuestionDto> McqQuestions,
        List<CodingQuestionDto> CodingQuestions
    );

    public sealed record McqQuestionDto(
        int QuestionId,
        string QuestionText,
        List<McqOptionsDto> Options
    );

    public sealed record McqOptionsDto(int OptionId, string OptionText);

    public sealed record CodingQuestionDto(
        int QuestionId,
        string QuesTitle,
        string QuestionText,
        List<TestCaseDto> TestCases,
        List<CodingQuestionTemplateDto> Templates
    );

    public sealed record CodingQuestionTemplateDto(
        int LanguageId,
        string LanguageName,
        string DefualtCode
    );

    public sealed record TestCaseDto(int TestCaseId, string Input, string Output);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CvFile).NotNull().WithMessage("CV file is required.");

            RuleFor(x => x.JobDescription)
                .NotEmpty()
                .WithMessage("Job description is required.")
                .When(x => x.JobDescription is not null);
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
            using var cvStream = request.CvFile.OpenReadStream();
            var aiRequest = new CandidateProfileRequest(cvStream, request.JobDescription);
            var aiResponse = await aiServiceClient.AnalyzeProfileAsync(
                aiRequest,
                cancellationToken
            );
            if (aiResponse?.Data is null)
            {
                return new BadRequestError(
                    "Failed to extract profile data. Please ensure the CV is a valid, text-readable PDF and try again."
                );
            }

            var detectedTrack = aiResponse.Data.TrackName?.ToLower().Trim();
            var detectedLevel = aiResponse.Data.Level;
            var detectedSkills = aiResponse
                .Data.TechnicalSkills.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(SkillNormalizer.Normalize)
                .ToList();

            var seniorityFallbackOrder = GetSeniorityFallbackOrder(detectedLevel);

            const int totalMcqNeeded = 10;
            const int totalCodingNeeded = 2;

            var mcqBaseQuery = context
                .Questions.AsNoTracking()
                .Where(q => q.QuestionType == QuestionType.MCQ)
                .Where(q =>
                    q.Skills.Any(s => s.Tracks.Any(t => t.Name.ToLower() == detectedTrack))
                    && q.Skills.Any(s => detectedSkills.Contains(s.NormalizedName))
                );

            var codingBaseQuery = context
                .Questions.AsNoTracking()
                .Where(q => q.QuestionType == QuestionType.Coding);

            var mcqQuestions = new List<McqQuestionDto>();
            foreach (var level in seniorityFallbackOrder)
            {
                if (mcqQuestions.Count >= totalMcqNeeded)
                    break;
                int neededCount = totalMcqNeeded - mcqQuestions.Count;

                var fetched = await GetMcqProjection(
                        mcqBaseQuery.Where(q => q.SeniorityLevel == level)
                    )
                    .Take(neededCount)
                    .ToListAsync(cancellationToken);

                mcqQuestions.AddRange(fetched);
            }

            var codingQuestions = new List<CodingQuestionDto>();
            foreach (var level in seniorityFallbackOrder)
            {
                if (codingQuestions.Count >= totalCodingNeeded)
                {
                    break;
                }
                int neededCount = totalCodingNeeded - codingQuestions.Count;

                var fetched = await GetCodingProjection(
                        codingBaseQuery.Where(q => q.SeniorityLevel == level)
                    )
                    .Take(neededCount)
                    .ToListAsync(cancellationToken);

                codingQuestions.AddRange(fetched);
            }

            var session = new InterviewSession
            {
                UserId = request.UserId,
                StartDate = DateTime.UtcNow,
                Answers = mcqQuestions
                    .Select(q => new SessionAnswer { QuestionId = q.QuestionId })
                    .Concat(
                        codingQuestions.Select(q => new SessionAnswer { QuestionId = q.QuestionId })
                    )
                    .ToList(),
            };

            context.InterviewSessions.Add(session);
            await context.SaveChangesAsync(cancellationToken);

            return new Response(session.Id, mcqQuestions, codingQuestions);
        }

        private static IQueryable<McqQuestionDto> GetMcqProjection(IQueryable<Question> query) =>
            query
                .Where(q => q.QuestionType == QuestionType.MCQ)
                .OrderBy(q => EF.Functions.Random())
                .Select(q => new McqQuestionDto(
                    q.Id,
                    q.Text,
                    q.Options.Select(o => new McqOptionsDto(o.Id, o.OptionText)).ToList()
                ))
                .AsSplitQuery();

        private static IQueryable<CodingQuestionDto> GetCodingProjection(
            IQueryable<Question> query
        ) =>
            query
                .Where(q => q.QuestionType == QuestionType.Coding)
                .OrderBy(q => EF.Functions.Random())
                .Select(q => new CodingQuestionDto(
                    q.Id,
                    q.Title,
                    q.Text,
                    q.TestCases.Where(tc => !tc.IsHidden)
                        .Select(tc => new TestCaseDto(tc.Id, tc.Input, tc.Output))
                        .ToList(),
                    q.Templates.Select(t => new CodingQuestionTemplateDto(
                            t.LanguageId,
                            Judge0Languages.GetName(t.LanguageId),
                            t.DefaultCode
                        ))
                        .ToList()
                ))
                .AsSplitQuery();

        private static string[] GetSeniorityFallbackOrder(string? level) =>
            level switch
            {
                SeniorityLevels.Senior =>
                [
                    SeniorityLevels.Senior,
                    SeniorityLevels.MidLevel,
                    SeniorityLevels.Junior,
                ],
                SeniorityLevels.MidLevel =>
                [
                    SeniorityLevels.MidLevel,
                    SeniorityLevels.Junior,
                    SeniorityLevels.Senior,
                ],
                SeniorityLevels.Junior =>
                [
                    SeniorityLevels.Junior,
                    SeniorityLevels.MidLevel,
                    SeniorityLevels.Senior,
                ],
                _ => [SeniorityLevels.MidLevel, SeniorityLevels.Junior, SeniorityLevels.Senior],
            };
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/interview-sessions",
                    async (
                        [FromForm] IFormFile cvFile,
                        [FromForm] string? jobDescription,
                        IMediator mediator,
                        ClaimsPrincipal user
                    ) =>
                    {
                        var userId = user.GetUserId();
                        if (string.IsNullOrEmpty(userId))
                            return Results.Unauthorized();

                        var request = new Request(cvFile, jobDescription)
                        {
                            UserId = int.Parse(userId),
                        };
                        var response = await mediator.Send(request);
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Interviews")
                .RequireAuthorization()
                .DisableAntiforgery();
        }
    }
}
