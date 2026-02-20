using FluentValidation;
using Microsoft.Extensions.Configuration;
using MockMate.Api.Configuration;
using MockMate.Api.Services.StorageService;

namespace MockMate.Api.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var assembly = typeof(Program).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.Configure<CloudinaryOptions>(
            configuration.GetSection(CloudinaryOptions.SectionName)
        );
        services.AddScoped<IImageStorageService, CloudinaryStorageService>();
        services.AddHttpContextAccessor();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAll",
                policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                }
            );
        });

        return services;
    }
}
