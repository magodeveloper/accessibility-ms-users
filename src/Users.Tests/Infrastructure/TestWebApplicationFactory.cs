using System.Text;
using System.Security.Claims;
using Users.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Users.Tests.Infrastructure
{
    public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Establecer el entorno de prueba
            builder.UseEnvironment("TestEnvironment");

            // Configurar las opciones de JWT directamente en el builder ANTES de que se construya
            builder.UseSetting("JwtSettings:SecretKey", "KvAuy4?q6DwCSl9Mn+7patFUeX-I^&x5@8%G1d!zkW0iQb2oEhTsP#RYfZNOJ=rc");
            builder.UseSetting("JwtSettings:Issuer", "https://api.accessibility.company.com/users");
            builder.UseSetting("JwtSettings:Audience", "https://accessibility.company.com");
            builder.UseSetting("JwtSettings:ExpiryHours", "24");

            // Configuración adicional
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Agregar configuración en memoria para sobrescribir otros valores si es necesario
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "TestEnvironment",
                    ["Environment"] = "TestEnvironment",
                    ["JwtSettings:SecretKey"] = "KvAuy4?q6DwCSl9Mn+7patFUeX-I^&x5@8%G1d!zkW0iQb2oEhTsP#RYfZNOJ=rc",
                    ["JwtSettings:Issuer"] = "https://api.accessibility.company.com/users",
                    ["JwtSettings:Audience"] = "https://accessibility.company.com",
                    ["JwtSettings:ExpiryHours"] = "24",
                    ["ConnectionStrings:UsersDb"] = "Host=localhost;Database=TestUsersDb;Username=testuser;Password=testpass"
                });

                // Agregar configuración de test específica (opcional)
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

        /// <summary>
        /// Crea un HttpClient con headers de autenticación X-User-* para simular
        /// peticiones autenticadas por el Gateway.
        /// </summary>
        /// <param name="userId">ID del usuario autenticado (default: 1)</param>
        /// <param name="email">Email del usuario autenticado (default: test@example.com)</param>
        /// <param name="role">Rol del usuario autenticado (default: Admin)</param>
        /// <param name="userName">Nombre del usuario autenticado (default: TestUser)</param>
        /// <returns>HttpClient configurado con headers de autenticación</returns>
        public HttpClient CreateAuthenticatedClient(int userId = 1, string email = "test@example.com",
            string role = "Admin", string userName = "TestUser")
        {
            var client = CreateClient();

            // Agregar headers X-User-* para UserContextMiddleware
            client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
            client.DefaultRequestHeaders.Add("X-User-Email", email);
            client.DefaultRequestHeaders.Add("X-User-Role", role);
            client.DefaultRequestHeaders.Add("X-User-Name", userName);

            // Generar y agregar token JWT para autenticación
            var token = GenerateJwtToken(userId, email, role, userName);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            return client;
        }

        /// <summary>
        /// Genera un token JWT de prueba válido usando la misma configuración que la aplicación.
        /// </summary>
        private static string GenerateJwtToken(int userId, string email, string role, string userName)
        {
            var secretKey = "KvAuy4?q6DwCSl9Mn+7patFUeX-I^&x5@8%G1d!zkW0iQb2oEhTsP#RYfZNOJ=rc";
            var issuer = "https://api.accessibility.company.com/users";
            var audience = "https://accessibility.company.com";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
