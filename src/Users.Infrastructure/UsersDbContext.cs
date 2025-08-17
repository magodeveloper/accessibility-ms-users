using Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Users.Infrastructure;

public sealed class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Preference> Preferences => Set<Preference>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // USERS
        b.Entity<User>(e =>
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
        b.Entity<Session>(e =>
        {
            e.ToTable("SESSIONS");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.TokenHash).HasColumnName("token_hash")
                .HasMaxLength(64) // sha256 (hex)
                .IsRequired()
                .UseCollation("ascii_bin");
            e.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ux_sessions_token_hash");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        });

        // PREFERENCES
        b.Entity<Preference>(e =>
        {
            e.ToTable("PREFERENCES");
            e.HasKey(x => x.Id);

            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

            e.Property(x => x.WcagVersion)
               .HasColumnName("wcag_version")
               .HasConversion(WcagVersionConverter)
               .HasMaxLength(3)
               .IsRequired();

            e.Property(x => x.WcagLevel).HasColumnName("wcag_level")
              .HasConversion(
                  v => v.ToString(), // A, AA, AAA
                  v => Enum.Parse<WcagLevel>(v)
              ).HasMaxLength(3).IsRequired();

            e.Property(x => x.Language).HasColumnName("language")
              .HasConversion(
                  v => v.ToString(), // es/en
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
              .HasConversion(v => v.ToString(), v => Enum.Parse<AiResponseLevel>(v))
              .HasMaxLength(12).HasDefaultValue(AiResponseLevel.intermediate);

            e.Property(x => x.FontSize).HasColumnName("font_size");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }

    private static readonly ValueConverter<WcagVersion, string> WcagVersionConverter =
        new(
            v => WcagVersionToString(v),
            s => StringToWcagVersion(s)
        );

    private static string WcagVersionToString(WcagVersion v)
    {
        if (v == WcagVersion._2_0) return "2.0";
        if (v == WcagVersion._2_1) return "2.1";
        return "2.2";
    }

    private static WcagVersion StringToWcagVersion(string s)
    {
        if (s == "2.0") return WcagVersion._2_0;
        if (s == "2.1") return WcagVersion._2_1;
        return WcagVersion._2_2;
    }
}