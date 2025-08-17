using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Users.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
                 ?? "server=127.0.0.1;port=3306;database=ms_users;user=msu;password=msupass;TreatTinyAsBoolean=false";

        services.AddDbContext<UsersDbContext>(opt =>
        {
            opt.UseMySql(
                cs,
                ServerVersion.AutoDetect(cs),
                o => o.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: System.TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null
                )
            );
        });

        return services;
    }
}