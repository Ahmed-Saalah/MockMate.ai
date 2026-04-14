namespace MockMate.Api.Clients.AiService.Dtos;

public record FeedbackRequest(
    int InterviewId,
    List<CodingAnswerDto> CodingAnswers,
    List<McqAnswerDto> McqAnswers
);

public record CodingAnswerDto(
    string QuestionTitle,
    string QuestionText,
    string Language,
    string SourceCode,
    string Judge0Status,
    decimal Score
);

public record McqAnswerDto(string QuestionText, string CandidateAnswer, bool IsCorrect);
