using Prometheus;

namespace Users.Api.Services;

/// <summary>
/// Servicio para recolectar y exponer métricas de negocio personalizadas de Users
/// </summary>
public interface IUserMetricsService
{
    void RecordUserRegistration(bool success);
    void RecordUserLogin(bool success);
    void RecordUserDeletion();
    void RecordSessionCreation();
    void RecordSessionDeletion();
    void RecordPreferenceUpdate();
    void RecordPasswordReset();
    void RecordApiRequest(string endpoint, string method, int statusCode, double durationMs);
}

public class UserMetricsService : IUserMetricsService
{
    // Contadores
    private static readonly Counter UserRegistrations = Metrics
        .CreateCounter(
            "users_registrations_total",
            "Total number of user registrations",
            new CounterConfiguration
            {
                LabelNames = new[] { "result" } // success, failure
            });

    private static readonly Counter UserLogins = Metrics
        .CreateCounter(
            "users_logins_total",
            "Total number of user login attempts",
            new CounterConfiguration
            {
                LabelNames = new[] { "result" } // success, failure
            });

    private static readonly Counter UserDeletions = Metrics
        .CreateCounter(
            "users_deletions_total",
            "Total number of user deletions");

    private static readonly Counter SessionsCreated = Metrics
        .CreateCounter(
            "users_sessions_created_total",
            "Total number of sessions created");

    private static readonly Counter SessionsDeleted = Metrics
        .CreateCounter(
            "users_sessions_deleted_total",
            "Total number of sessions deleted");

    private static readonly Counter PreferencesUpdated = Metrics
        .CreateCounter(
            "users_preferences_updated_total",
            "Total number of preference updates");

    private static readonly Counter PasswordResets = Metrics
        .CreateCounter(
            "users_password_resets_total",
            "Total number of password reset requests");

    // Histogramas para latencias
    private static readonly Histogram ApiRequestDuration = Metrics
        .CreateHistogram(
            "users_api_request_duration_milliseconds",
            "Duration of API requests in milliseconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "endpoint", "method", "status_code" },
                Buckets = Histogram.ExponentialBuckets(10, 2, 8) // 10ms, 20ms, 40ms, 80ms, 160ms, 320ms, 640ms, 1280ms
            });

    // Gauges para métricas instantáneas
    private static readonly Gauge ActiveSessions = Metrics
        .CreateGauge(
            "users_active_sessions",
            "Current number of active user sessions");

    public void RecordUserRegistration(bool success)
    {
        UserRegistrations.WithLabels(success ? "success" : "failure").Inc();
    }

    public void RecordUserLogin(bool success)
    {
        UserLogins.WithLabels(success ? "success" : "failure").Inc();
    }

    public void RecordUserDeletion()
    {
        UserDeletions.Inc();
    }

    public void RecordSessionCreation()
    {
        SessionsCreated.Inc();
        ActiveSessions.Inc();
    }

    public void RecordSessionDeletion()
    {
        SessionsDeleted.Inc();
        ActiveSessions.Dec();
    }

    public void RecordPreferenceUpdate()
    {
        PreferencesUpdated.Inc();
    }

    public void RecordPasswordReset()
    {
        PasswordResets.Inc();
    }

    public void RecordApiRequest(string endpoint, string method, int statusCode, double durationMs)
    {
        ApiRequestDuration
            .WithLabels(endpoint, method, statusCode.ToString())
            .Observe(durationMs);
    }
}
