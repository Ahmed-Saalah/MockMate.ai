using FluentValidation;
using MockMate.Api.Common.Behaviors;
using MockMate.Api.Configuration;
using MockMate.Api.Services.CodeExecutionService;
using MockMate.Api.Services.StorageService;
using MockMate.Api.Services.EmailService;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MockMate.Api.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var assembly = typeof(Program).Assembly;
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);
        services.Configure<CloudinaryOptions>(
            configuration.GetSection(CloudinaryOptions.SectionName)
        );
        services.AddScoped<IImageStorageService, CloudinaryStorageService>();
        services.AddScoped<ICodeExecutionService, CodeExecutionService>();
        
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddMemoryCache();
        
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("ForgotPasswordLimiter", opt =>
            {
                opt.PermitLimit = 3;
                opt.Window = TimeSpan.FromMinutes(5);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

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
