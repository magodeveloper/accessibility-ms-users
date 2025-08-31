using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure.Data;

namespace Users.Tests.Infrastructure
{
    public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("TestEnvironment");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Limpiar configuraciones existentes y agregar configuración específica de tests
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "TestEnvironment",
                    ["Environment"] = "TestEnvironment"
                });

                // Agregar configuración de test específica
                config.AddJsonFile("appsettings.Test.json", optional: true);
            });

            builder.ConfigureServices(services =>
            {
                // Buscar y remover TODOS los DbContext relacionados
                var descriptors = services.Where(d => d.ServiceType.Name.Contains("DbContext") ||
                                                     d.ServiceType == typeof(DbContextOptions<UsersDbContext>) ||
                                                     d.ServiceType == typeof(UsersDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Configurar explícitamente el DbContext para usar InMemory
                services.AddDbContext<UsersDbContext>(options =>
                    options.UseInMemoryDatabase("TestDatabase"));

                // Crear la base de datos en memoria
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
                context.Database.EnsureCreated();
            });
        }
    }
}
