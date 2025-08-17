using System.Threading.Tasks;
using System.Collections.Generic;

namespace Users.Application.Services.Session
{
    public interface ISessionService
    {
        Task<Users.Domain.Entities.Session> CreateSessionAsync(Users.Domain.Entities.Session session);
        Task<Users.Application.Dtos.SessionReadDto?> GetSessionByIdAsync(int id);
        Task<IEnumerable<Users.Application.Dtos.SessionReadDto>> GetAllSessionsAsync();
        Task<IEnumerable<Users.Application.Dtos.SessionReadDto>> GetSessionsByUserIdAsync(int userId);
        Task<Users.Domain.Entities.Session?> UpdateSessionAsync(Users.Domain.Entities.Session session);
        Task<bool> DeleteSessionAsync(int id);
        Task<bool> DeleteSessionsByUserIdAsync(int userId);
    }
}