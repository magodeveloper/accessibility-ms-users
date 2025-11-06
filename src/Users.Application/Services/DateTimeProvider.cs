using System;

namespace Users.Application.Services;

public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

public class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo EcuadorTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EcuadorTimeZone);

    public DateTime UtcNow => DateTime.UtcNow;
}
