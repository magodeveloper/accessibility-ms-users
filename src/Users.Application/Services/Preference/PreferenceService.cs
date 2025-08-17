using Users.Application;
using System.Threading.Tasks;
using Users.Infrastructure.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Users.Application.Services.Preference
{
    public sealed class PreferenceService : IPreferenceService
    {
        private readonly UsersDbContext _db;

        public PreferenceService(UsersDbContext db)
        {
            _db = db;
        }

        public async Task<Users.Domain.Entities.Preference> CreatePreferenceAsync(Users.Domain.Entities.Preference preference)
        {
            // Evitar duplicados por user_id
            var exists = await _db.Preferences.AnyAsync(p => p.UserId == preference.UserId);
            if (exists) throw new InvalidOperationException(Localization.Get("Error_PreferencesExist"));

            var now = DateTime.UtcNow;
            preference.CreatedAt = now;
            preference.UpdatedAt = now;
            _db.Preferences.Add(preference);
            await _db.SaveChangesAsync();
            return preference;
        }

        public async Task<Users.Domain.Entities.Preference?> GetPreferenceByIdAsync(int id)
        {
            return await _db.Preferences.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Users.Domain.Entities.Preference>> GetAllPreferencesAsync()
        {
            return await _db.Preferences.Include(p => p.User).ToListAsync();
        }

        public async Task<Users.Domain.Entities.Preference?> UpdatePreferenceAsync(Users.Domain.Entities.Preference preference)
        {
            var p = await _db.Preferences.FirstOrDefaultAsync(x => x.UserId == preference.UserId);
            if (p is null) return null;

            // Actualizar campos
            p.WcagVersion = preference.WcagVersion;
            p.WcagLevel = preference.WcagLevel;
            p.Language = preference.Language;
            p.VisualTheme = preference.VisualTheme;
            p.ReportFormat = preference.ReportFormat;
            p.NotificationsEnabled = preference.NotificationsEnabled;
            p.AiResponseLevel = preference.AiResponseLevel;
            p.FontSize = preference.FontSize;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return p;
        }

        public async Task<bool> DeletePreferenceAsync(int id)
        {
            var p = await _db.Preferences.FindAsync(id);
            if (p is null) return false;
            _db.Preferences.Remove(p);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}