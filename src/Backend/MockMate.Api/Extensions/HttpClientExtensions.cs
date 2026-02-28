using MockMate.Api.Clients.AiService;
using MockMate.Api.Clients.AiService.Interfaces;

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

        return services;
    }
}
