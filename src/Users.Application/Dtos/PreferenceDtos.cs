namespace Users.Application.Dtos
{
    public record PreferenceReadDto(
        int Id,
        int UserId,
        string WcagVersion,
        string WcagLevel,
        string Language,
        string VisualTheme,
        string ReportFormat,
        bool NotificationsEnabled,
        string? AiResponseLevel,
        int FontSize,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public record PreferenceCreateDto(int UserId, string WcagVersion, string WcagLevel, string? Language, string? VisualTheme, string? ReportFormat, bool? NotificationsEnabled, string? AiResponseLevel, int? FontSize);
    public record PreferencePatchDto(string? WcagVersion, string? WcagLevel, string? Language, string? VisualTheme, string? ReportFormat, bool? NotificationsEnabled, string? AiResponseLevel, int? FontSize);
    public record PreferenceUserPatchDto(
        string? WcagVersion,
        string? WcagLevel,
        string? Language,
        string? VisualTheme,
        string? ReportFormat,
        bool? NotificationsEnabled,
        string? AiResponseLevel,
        int? FontSize,
        // Solo campos de usuario requeridos
        string? Nickname,
        string? Name,
        string? Lastname,
        string? Email,
        string? Password
    );
}