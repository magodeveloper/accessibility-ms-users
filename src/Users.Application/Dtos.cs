namespace Users.Application;

public record UserCreateDto(string Nickname, string Name, string Lastname, string Email, string Password);
public record UserReadDto(int Id, string Nickname, string Name, string Lastname, string Email, string Role, string Status, bool EmailConfirmed, DateTime? LastLogin, DateTime RegistrationDate, DateTime CreatedAt, DateTime UpdatedAt);
public record UserPatchDto(string? Nickname, string? Name, string? Lastname, string? Role, string? Status, bool? EmailConfirmed);

public record LoginDto(string Email, string Password);
public record LoginResponseDto(string Token, DateTime? ExpiresAt);

public record ResetPasswordRequestDto(string Email);
public record ResetPasswordDto(string Email, string NewPassword); // simple (sin código), para demo

public record PreferenceCreateDto(int UserId, string WcagVersion, string WcagLevel, string? Language, string? VisualTheme, string? ReportFormat, bool? NotificationsEnabled, string? AiResponseLevel, int? FontSize);
public record PreferencePatchDto(string? WcagVersion, string? WcagLevel, string? Language, string? VisualTheme, string? ReportFormat, bool? NotificationsEnabled, string? AiResponseLevel, int? FontSize);