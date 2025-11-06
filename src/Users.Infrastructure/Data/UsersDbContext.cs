using Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Users.Infrastructure.Data;

public sealed class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Preference> Preferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Convertidor de DateTime para forzar DateTimeKind.Local
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Local),
            v => DateTime.SpecifyKind(v, DateTimeKind.Local)
        );

        // Aplicar el convertidor a todas las propiedades DateTime
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
            }
        }

        // USERS
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nickname).HasColumnName("nickname").HasMaxLength(15).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(30).IsRequired();
            e.Property(x => x.Lastname).HasColumnName("lastname").HasMaxLength(30).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(60).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Nickname).IsUnique();
            e.Property(x => x.Password).HasColumnName("password").HasMaxLength(60).IsRequired();

            e.Property(x => x.Role).HasColumnName("role")
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<UserRole>(v))
                .HasMaxLength(5)
                .IsRequired();

            e.Property(x => x.Status).HasColumnName("status")
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<UserStatus>(v))
                .HasMaxLength(8)
                .IsRequired();

            e.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
            e.Property(x => x.LastLogin).HasColumnName("last_login");
            e.Property(x => x.RegistrationDate).HasColumnName("registration_date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasMany(x => x.Sessions).WithOne(s => s.User).HasForeignKey(s => s.UserId);
            e.HasOne(x => x.Preference).WithOne(p => p.User).HasForeignKey<Preference>(p => p.UserId);
        });

        // SESSIONS
        modelBuilder.Entity<Session>(e =>
        {
            e.ToTable("SESSIONS");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.TokenHash).HasColumnName("token_hash")
                .HasMaxLength(64)
                .IsRequired()
                .UseCollation("ascii_bin");
            e.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ux_sessions_token_hash");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        });

        // PREFERENCES
        modelBuilder.Entity<Preference>(e =>
        {
            e.ToTable("PREFERENCES");
            e.HasKey(x => x.Id);

            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

            e.Property(x => x.WcagVersion)
                .HasColumnName("wcag_version")
                .HasMaxLength(3)
                .IsRequired();

            e.Property(x => x.WcagLevel).HasColumnName("wcag_level")
              .HasConversion(
                  v => v.ToString(),
                  v => Enum.Parse<WcagLevel>(v)
              ).HasMaxLength(3).IsRequired();

            e.Property(x => x.Language).HasColumnName("language")
              .HasConversion(
                  v => v.ToString(),
                  v => Enum.Parse<Language>(v)
              ).HasMaxLength(2).HasDefaultValue(Language.es);

            e.Property(x => x.VisualTheme).HasColumnName("visual_theme")
              .HasConversion(v => v.ToString(), v => Enum.Parse<VisualTheme>(v))
              .HasMaxLength(5).HasDefaultValue(VisualTheme.light);

            e.Property(x => x.ReportFormat).HasColumnName("report_format")
              .HasConversion(v => v.ToString(), v => Enum.Parse<ReportFormat>(v))
              .HasMaxLength(5).HasDefaultValue(ReportFormat.pdf);

            e.Property(x => x.NotificationsEnabled).HasColumnName("notifications_enabled");
            e.Property(x => x.AiResponseLevel).HasColumnName("ai_response_level")
                .HasConversion(
                        v => v != null ? v.ToString() : null,
                        v => v != null ? Enum.Parse<AiResponseLevel>(v) : (AiResponseLevel?)null)
                .HasMaxLength(12)
                .HasDefaultValue(AiResponseLevel.intermediate);

            e.Property(x => x.FontSize).HasColumnName("font_size");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }
}