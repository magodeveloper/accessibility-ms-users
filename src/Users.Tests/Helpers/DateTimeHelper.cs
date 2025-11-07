namespace Users.Tests.Helpers;

/// <summary>
/// Helper para manejar fechas en los tests considerando la zona horaria de Ecuador (UTC-5)
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Obtiene la hora actual de Ecuador (UTC-5)
    /// </summary>
    public static DateTime EcuadorNow => DateTime.UtcNow.AddHours(-5);

    /// <summary>
    /// Convierte una fecha UTC a hora de Ecuador
    /// </summary>
    public static DateTime ToEcuadorTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Utc)
        {
            return utcDateTime.AddHours(-5);
        }
        return utcDateTime;
    }
}
