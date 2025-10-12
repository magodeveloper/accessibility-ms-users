namespace Users.Api.Middleware;

/// <summary>
/// Middleware that validates the X-Gateway-Secret header to ensure requests
/// are coming from the Gateway and not directly to the microservice.
/// </summary>
public class GatewaySecretValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewaySecretValidationMiddleware> _logger;
    private readonly string? _expectedSecret;
    private readonly IWebHostEnvironment _environment;

    public GatewaySecretValidationMiddleware(
        RequestDelegate next,
        ILogger<GatewaySecretValidationMiddleware> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _expectedSecret = configuration["Gateway:Secret"] ?? configuration["GATEWAY_SECRET"];

        // El warning es normal en entornos de test - no es un problema
        if (string.IsNullOrEmpty(_expectedSecret))
        {
            _logger.LogWarning("Gateway:Secret not configured. Gateway secret validation will be disabled.");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for health check endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics"))
        {
            _logger.LogDebug("Gateway secret validation skipped for health/metrics endpoint: {Path}", context.Request.Path);
            await _next(context);
            return;
        }

        // Skip validation in Test environment
        if (_environment.EnvironmentName == "TestEnvironment")
        {
            _logger.LogDebug("Gateway secret validation skipped for TestEnvironment");
            await _next(context);
            return;
        }

        // Skip validation if secret is not configured
        if (string.IsNullOrEmpty(_expectedSecret))
        {
            await _next(context);
            return;
        }

        // Get the X-Gateway-Secret header
        if (!context.Request.Headers.TryGetValue("X-Gateway-Secret", out var gatewaySecret))
        {
            _logger.LogWarning("Request rejected: Missing X-Gateway-Secret header. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Direct access to microservice is not allowed. Please use the Gateway."
            });
            return;
        }

        // Validate the secret
        if (gatewaySecret != _expectedSecret)
        {
            _logger.LogWarning("Request rejected: Invalid X-Gateway-Secret header. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Invalid Gateway secret. Please use the Gateway."
            });
            return;
        }

        _logger.LogDebug("Gateway secret validated successfully for path: {Path}", context.Request.Path);

        // Secret is valid, continue with the request
        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering the GatewaySecretValidationMiddleware
/// </summary>
public static class GatewaySecretValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewaySecretValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GatewaySecretValidationMiddleware>();
    }
}
