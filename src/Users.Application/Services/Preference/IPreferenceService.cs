using System.Threading.Tasks;
using System.Collections.Generic;

namespace Users.Application.Services.Preference
{
    public interface IPreferenceService
    {
        Task<Users.Domain.Entities.Preference> CreatePreferenceAsync(Users.Domain.Entities.Preference preference);
        Task<Users.Domain.Entities.Preference?> GetPreferenceByIdAsync(int id);
        Task<IEnumerable<Users.Domain.Entities.Preference>> GetAllPreferencesAsync();
        Task<Users.Domain.Entities.Preference?> UpdatePreferenceAsync(Users.Domain.Entities.Preference preference);
        Task<bool> DeletePreferenceAsync(int id);
    }
}