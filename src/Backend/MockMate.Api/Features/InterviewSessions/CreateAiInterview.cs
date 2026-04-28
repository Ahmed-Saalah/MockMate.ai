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

public sealed class CreateAiInterview
{
    public sealed record Request(IFormFile CvFile, string? JobDescription)
        : IRequest<Result<CreateInterview.Response>>
    {
        [JsonIgnore]
        public int UserId { get; set; }
    }

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
        : IRequestHandler<Request, Result<CreateInterview.Response>>
    {
        public async Task<Result<CreateInterview.Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            using var cvStream = request.CvFile.OpenReadStream();
            var aiRequest = new CandidateProfileRequest(cvStream, request.JobDescription);
            var aiResponse = await aiServiceClient.GenerateInterviewAsync(
                aiRequest,
                cancellationToken
            );

            if (aiResponse is null)
            {
                return new BadRequestError(
                    "Failed to generate AI interview. Please ensure the CV is valid."
                );
            }

            var detectedTrack = aiResponse.TrackName.ToLower().Trim();
            var detectedLevel = aiResponse.SeniorityLevel;

            // 1. Upsert Track
            var track = await context
                .Tracks.Include(t => t.Skills)
                .FirstOrDefaultAsync(t => t.Name.ToLower() == detectedTrack, cancellationToken);
            if (track is null)
            {
                track = new Track { Name = aiResponse.TrackName };
                context.Tracks.Add(track);
            }

            // 2. Upsert Skills
            var dbSkills = new List<Skill>();
            foreach (var rawSkill in aiResponse.DetectedSkills)
            {
                if (string.IsNullOrWhiteSpace(rawSkill))
                    continue;
                var normalized = SkillNormalizer.Normalize(rawSkill);
                var existingSkill = await context.Skills.FirstOrDefaultAsync(
                    s => s.NormalizedName == normalized,
                    cancellationToken
                );

                if (existingSkill is not null)
                {
                    dbSkills.Add(existingSkill);
                }
                else
                {
                    var newSkill = new Skill { Name = rawSkill, NormalizedName = normalized };
                    context.Skills.Add(newSkill);
                    dbSkills.Add(newSkill);
                }
            }

            // Save new track and skills to resolve IDs before assigning them
            await context.SaveChangesAsync(cancellationToken);

            // Ensure the track is associated with the skills
            foreach (var skill in dbSkills)
            {
                if (!track.Skills.Any(s => s.Id == skill.Id))
                {
                    track.Skills.Add(skill);
                }
            }

            // 3. Deduplicate & Map Questions
            var mcqQuestions = new List<CreateInterview.McqQuestionDto>();
            var mcqQuestionEntities = new List<Question>();
            foreach (var q in aiResponse.McqQuestions)
            {
                var title = q.Title.Trim();
                var text = q.Text.Trim();

                var existingQ = await context
                    .Questions.Include(x => x.Options)
                    .FirstOrDefaultAsync(
                        x =>
                            x.QuestionType == QuestionType.MCQ
                            && (x.Title == title || x.Text == text),
                        cancellationToken
                    );

                Question targetQ;
                if (existingQ is not null)
                {
                    targetQ = existingQ;
                }
                else
                {
                    targetQ = new Question
                    {
                        Title = q.Title,
                        Text = q.Text,
                        SeniorityLevel = detectedLevel,
                        QuestionType = QuestionType.MCQ,
                        IsAiGenerated = true,
                        Options = q
                            .Options.Select(o => new McqOption
                            {
                                OptionText = o.OptionText,
                                IsCorrect = o.IsCorrect,
                            })
                            .ToList(),
                    };

                    foreach (var s in dbSkills)
                        targetQ.Skills.Add(s);
                    context.Questions.Add(targetQ);
                }
                mcqQuestionEntities.Add(targetQ);
            }

            var codingQuestions = new List<CreateInterview.CodingQuestionDto>();
            var codingQuestionEntities = new List<Question>();
            foreach (var q in aiResponse.CodingQuestions)
            {
                var title = q.Title.Trim();
                var text = q.Text.Trim();

                var existingQ = await context
                    .Questions.Include(x => x.TestCases)
                    .Include(x => x.Templates)
                    .FirstOrDefaultAsync(
                        x =>
                            x.QuestionType == QuestionType.Coding
                            && (x.Title == title || x.Text == text),
                        cancellationToken
                    );

                Question targetQ;
                if (existingQ is not null)
                {
                    targetQ = existingQ;
                }
                else
                {
                    targetQ = new Question
                    {
                        Title = q.Title,
                        Text = q.Text,
                        SeniorityLevel = detectedLevel,
                        QuestionType = QuestionType.Coding,
                        IsAiGenerated = true,
                        TestCases = q
                            .TestCases.Select(tc => new TestCase
                            {
                                Input = tc.Input,
                                Output = tc.Output,
                                IsHidden = tc.IsHidden,
                            })
                            .ToList(),
                        Templates = q
                            .Templates.Select(t => new LanguageTemplate
                            {
                                LanguageId = t.LanguageId,
                                DefaultCode = t.DefaultCode,
                                DriverCode = t.DriverCode,
                                TimeLimit = 2.0m,
                                MemoryLimit = 128,
                            })
                            .ToList(),
                    };

                    foreach (var s in dbSkills)
                        targetQ.Skills.Add(s);
                    context.Questions.Add(targetQ);
                }
                codingQuestionEntities.Add(targetQ);
            }

            // Save new questions to generate IDs
            await context.SaveChangesAsync(cancellationToken);

            // Populate Response DTOs
            foreach (var q in mcqQuestionEntities)
            {
                mcqQuestions.Add(
                    new CreateInterview.McqQuestionDto(
                        q.Id,
                        q.Text,
                        q.Options.Select(o => new CreateInterview.McqOptionsDto(o.Id, o.OptionText))
                            .ToList()
                    )
                );
            }

            foreach (var q in codingQuestionEntities)
            {
                codingQuestions.Add(
                    new CreateInterview.CodingQuestionDto(
                        q.Id,
                        q.Title,
                        q.Text,
                        q.TestCases.Where(tc => !tc.IsHidden)
                            .Select(tc => new CreateInterview.TestCaseDto(
                                tc.Id,
                                tc.Input,
                                tc.Output
                            ))
                            .ToList(),
                        q.Templates.Select(t => new CreateInterview.CodingQuestionTemplateDto(
                                t.LanguageId,
                                Judge0Languages.GetName(t.LanguageId),
                                t.DefaultCode
                            ))
                            .ToList()
                    )
                );
            }

            // 4. Create Session
            var session = new InterviewSession
            {
                UserId = request.UserId,
                TrackName = aiResponse.TrackName,
                SeniorityLevel = aiResponse.SeniorityLevel,
                StartDate = DateTime.UtcNow,
                Answers = mcqQuestionEntities
                    .Select(q => new SessionAnswer { QuestionId = q.Id })
                    .Concat(
                        codingQuestionEntities.Select(q => new SessionAnswer { QuestionId = q.Id })
                    )
                    .ToList(),
            };

            context.InterviewSessions.Add(session);
            await context.SaveChangesAsync(cancellationToken);

            return new CreateInterview.Response(session.Id, mcqQuestions, codingQuestions);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/interview-sessions/ai",
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
