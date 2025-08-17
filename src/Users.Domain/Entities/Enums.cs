using System.Text.Json.Serialization;

namespace Users.Domain.Entities
{
    using System.Text.Json.Serialization;
    public enum UserRole { admin, user }
    public enum UserStatus { active, inactive, blocked }
    public enum WcagLevel { A, AA, AAA }
    public enum Language { es, en }
    public enum VisualTheme { light, dark }
    public enum ReportFormat { pdf, html, json, excel }
    public enum AiResponseLevel { basic, intermediate, detailed }
}