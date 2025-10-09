using Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.Api.HealthChecks
{
    /// <summary>
    /// Health check personalizado para verificar la conectividad y estado de la base de datos MySQL
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly UsersDbContext _dbContext;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(UsersDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar que la base de datos responda con una query simple
                var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

                if (!canConnect)
                {
                    _logger.LogWarning("Cannot connect to Users database");
                    return HealthCheckResult.Unhealthy(
                        "Cannot connect to the Users database",
                        data: new Dictionary<string, object>
                        {
                        { "database", _dbContext.Database.GetConnectionString() ?? "unknown" }
                        });
                }

                // Contar usuarios como verificación adicional
                var userCount = await _dbContext.Users.CountAsync(cancellationToken);

                _logger.LogDebug("Database health check passed. User count: {UserCount}", userCount);

                return HealthCheckResult.Healthy(
                    "Database is accessible and responsive",
                    data: new Dictionary<string, object>
                    {
                    { "database", _dbContext.Database.GetConnectionString() ?? "unknown" },
                    { "userCount", userCount },
                    { "timestamp", DateTime.UtcNow }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");

                return HealthCheckResult.Unhealthy(
                    "Database check failed",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                    { "error", ex.Message },
                    { "timestamp", DateTime.UtcNow }
                    });
            }
        }
    }
}