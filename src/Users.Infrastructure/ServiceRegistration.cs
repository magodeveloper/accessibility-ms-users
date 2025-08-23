using Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Users.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Detectar si estamos en entorno de tests
        var environmentName = config["ASPNETCORE_ENVIRONMENT"] ?? config["Environment"];

        if (environmentName == "TestEnvironment")
        {
            // Para tests, usar InMemory database
            services.AddDbContext<UsersDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
        }
        else
        {
            // Para producción/desarrollo, usar MySQL
            var cs = config.GetConnectionString("Default")
                     ?? "server=127.0.0.1;port=3306;database=usersdb;user=msuser;password=msupass;TreatTinyAsBoolean=false";

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
        }

        return services;
    }
}