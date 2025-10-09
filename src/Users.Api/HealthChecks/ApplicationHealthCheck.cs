using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.Api.HealthChecks
{
    /// <summary>
    /// Health check general de la aplicación que verifica su estado básico
    /// </summary>
    public class ApplicationHealthCheck : IHealthCheck
    {
        private readonly ILogger<ApplicationHealthCheck> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ApplicationHealthCheck(
            ILogger<ApplicationHealthCheck> logger,
            IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar si la aplicación está en proceso de shutdown
                if (_lifetime.ApplicationStopping.IsCancellationRequested)
                {
                    _logger.LogWarning("Application is stopping");
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "Application is shutting down",
                        data: new Dictionary<string, object>
                        {
                        { "status", "stopping" },
                        { "timestamp", DateTime.UtcNow }
                        }));
                }

                var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

                var data = new Dictionary<string, object>
            {
                { "status", "running" },
                { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" },
                { "uptimeSeconds", Math.Round(uptime.TotalSeconds, 0) },
                { "uptimeFormatted", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m" },
                { "timestamp", DateTime.UtcNow },
                { "machineName", Environment.MachineName },
                { "processId", Environment.ProcessId }
            };

                _logger.LogDebug("Application health check passed. Uptime: {Uptime}", data["uptimeFormatted"]);

                return Task.FromResult(HealthCheckResult.Healthy(
                    "Application is running normally",
                    data: data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application health check failed");

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Application check failed",
                    exception: ex));
            }
        }
    }
}