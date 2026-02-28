using System.Net.Http.Headers;
using MockMate.Api.Clients.AiService.Dtos;
using MockMate.Api.Clients.AiService.Interfaces;

namespace MockMate.Api.Clients.AiService;

public class AiServiceClient(HttpClient httpClient, ILogger<AiServiceClient> logger)
    : IAiServiceClient
{
    public async Task<CandidateProfileResponse?> AnalyzeProfileAsync(
        CandidateProfileRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var content = new MultipartFormDataContent();

            var pdfContent = new StreamContent(request.CV);
            pdfContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(pdfContent, "cv_file", "cv.pdf");

            var jobDescContent = new StringContent(request.JobDescription);
            content.Add(jobDescContent, "job_description");

            var response = await httpClient.PostAsync("/analyze", content, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CandidateProfileResponse>(
                cancellationToken: cancellationToken
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "HTTP error occurred while calling the AI Service for profile analysis."
            );
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An unexpected error occurred while communicating with the AI Service."
            );
            throw;
        }
    }
}
