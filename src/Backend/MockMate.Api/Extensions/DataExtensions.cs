using Microsoft.EntityFrameworkCore;
using MockMate.Api.Data;

namespace MockMate.Api.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
}
