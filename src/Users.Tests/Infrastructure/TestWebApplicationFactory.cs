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
            });

            builder.ConfigureServices(services =>
            {
                // Ya no necesitamos hacer nada aquí, porque ServiceRegistration.cs
                // detectará el entorno TestEnvironment y usará InMemory automáticamente

                // Solo asegurar que la base de datos se cree después del build
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
                context.Database.EnsureCreated();
            });
        }
    }
}
