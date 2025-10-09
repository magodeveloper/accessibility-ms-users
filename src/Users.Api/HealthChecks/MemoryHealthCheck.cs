using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.Api.HealthChecks
{
    /// <summary>
    /// Health check para monitorear el uso de memoria de la aplicación
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly ILogger<MemoryHealthCheck> _logger;
        private readonly long _threshold;

        public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger, long threshold = 1024L * 1024L * 1024L) // 1GB default
        {
            _logger = logger;
            _threshold = threshold;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
                var allocatedMB = allocatedBytes / 1024.0 / 1024.0;
                var thresholdMB = _threshold / 1024.0 / 1024.0;

                var data = new Dictionary<string, object>
            {
                { "allocatedMB", Math.Round(allocatedMB, 2) },
                { "thresholdMB", Math.Round(thresholdMB, 2) },
                { "gen0Collections", GC.CollectionCount(0) },
                { "gen1Collections", GC.CollectionCount(1) },
                { "gen2Collections", GC.CollectionCount(2) }
            };

                if (allocatedBytes >= _threshold)
                {
                    _logger.LogWarning("Memory usage is high: {AllocatedMB}MB / {ThresholdMB}MB",
                        Math.Round(allocatedMB, 2), Math.Round(thresholdMB, 2));

                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Memory usage is high: {Math.Round(allocatedMB, 2)}MB / {Math.Round(thresholdMB, 2)}MB",
                        data: data));
                }

                _logger.LogDebug("Memory health check passed: {AllocatedMB}MB", Math.Round(allocatedMB, 2));

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Memory usage is normal: {Math.Round(allocatedMB, 2)}MB",
                    data: data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Memory check failed",
                    exception: ex));
            }
        }
    }
}