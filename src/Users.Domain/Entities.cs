namespace Users.Domain;

public enum UserRole { admin, user }
public enum UserStatus { active, inactive, blocked }

public enum WcagVersion { _2_0, _2_1, _2_2 }
public enum WcagLevel { A, AA, AAA }
public enum Language { es, en }
public enum VisualTheme { light, dark }
public enum ReportFormat { pdf, html, json, excel }
public enum AiResponseLevel { basic, intermediate, detailed }

public sealed class User
{
    public int Id { get; set; }
    public string Nickname { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Lastname { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password /* bcrypt hash */ { get; set; } = default!;
    public UserRole Role { get; set; } = UserRole.user;
    public UserStatus Status { get; set; } = UserStatus.active;
    public bool EmailConfirmed { get; set; } = false;
    public DateTime? LastLogin { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Preference? Preference { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}

public sealed class Session
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash /* sha256 hex */ { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public User User { get; set; } = default!;
}

public sealed class Preference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public WcagVersion WcagVersion { get; set; }
    public WcagLevel WcagLevel { get; set; }
    public Language Language { get; set; } = Language.es;
    public VisualTheme VisualTheme { get; set; } = VisualTheme.light;
    public ReportFormat ReportFormat { get; set; } = ReportFormat.pdf;
    public bool NotificationsEnabled { get; set; } = true;
    public AiResponseLevel AiResponseLevel { get; set; } = AiResponseLevel.intermediate;
    public int FontSize { get; set; } = 14;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; } = default!;
}