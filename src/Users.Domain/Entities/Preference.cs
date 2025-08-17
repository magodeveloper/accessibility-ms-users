using System;

namespace Users.Domain.Entities
{
    public sealed class Preference
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string WcagVersion { get; set; }
        public WcagLevel WcagLevel { get; set; }
        public Language Language { get; set; } = Language.es;
        public VisualTheme VisualTheme { get; set; } = VisualTheme.light;
        public ReportFormat ReportFormat { get; set; } = ReportFormat.pdf;
        public bool NotificationsEnabled { get; set; } = true;
        public AiResponseLevel? AiResponseLevel { get; set; }
        public int FontSize { get; set; } = 14;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public User User { get; set; } = default!;
    }
}