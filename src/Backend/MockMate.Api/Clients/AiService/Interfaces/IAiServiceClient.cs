using MockMate.Api.Clients.AiService.Dtos;

namespace MockMate.Api.Clients.AiService.Interfaces;

public interface IAiServiceClient
{
    Task<CandidateProfileResponse?> AnalyzeProfileAsync(
        CandidateProfileRequest request,
        CancellationToken cancellationToken = default
    );
}
