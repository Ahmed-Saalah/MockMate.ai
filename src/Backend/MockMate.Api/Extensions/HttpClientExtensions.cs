using MockMate.Api.Clients.AiService;
using MockMate.Api.Clients.AiService.Interfaces;
using MockMate.Api.Clients.Judge0;
using MockMate.Api.Clients.Judge0.Interfaces;

namespace MockMate.Api.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddExternalHttpClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpClient<IAiServiceClient, AiServiceClient>(client =>
        {
            var url = configuration["AiService:BaseUrl"];

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(
                    nameof(url),
                    "AiService:BaseUrl is missing in appsettings.json"
                );
            }

            client.BaseAddress = new Uri(url);
            client.Timeout = TimeSpan.FromMinutes(1);
        });

        services.AddHttpClient<IJudge0Service, Judge0Service>(client =>
        {
            var url = configuration["Judge0:BaseUrl"];

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(
                    nameof(url),
                    "Judge0:BaseUrl is missing in appsettings.json"
                );
            }

            client.BaseAddress = new Uri(url.TrimEnd('/') + '/');
            client.Timeout = TimeSpan.FromMinutes(2);
            var apiKey = configuration["Judge0:ApiKey"];
            var apiHost = configuration["Judge0:ApiHost"];
            var hasKey = !string.IsNullOrEmpty(apiKey);
            var hasHost = !string.IsNullOrEmpty(apiHost);
            if (hasKey && hasHost)
            {
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", apiKey);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", apiHost);
            }
            else if (hasKey != hasHost)
            {
                throw new InvalidOperationException(
                    "Judge0 RapidAPI configuration is incomplete. "
                        + "Both 'Judge0:ApiKey' and 'Judge0:ApiHost' must be set together, or both must be left empty for a self-hosted instance."
                );
            }
        });

        return services;
    }
}
