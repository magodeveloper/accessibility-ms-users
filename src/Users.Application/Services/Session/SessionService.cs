using System.Threading.Tasks;
using Users.Application.Dtos;
using Users.Infrastructure.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Users.Application.Services.Session
{
    public sealed class SessionService : ISessionService
    {
        private readonly UsersDbContext _db;

        public SessionService(UsersDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<SessionReadDto>> GetSessionsByUserIdAsync(int userId)
        {
            var sessions = await _db.Sessions.Include(s => s.User).Where(s => s.UserId == userId).ToListAsync();
            return sessions.Select(MapToSessionReadDto).ToList();
        }

        public async Task<Users.Domain.Entities.Session> CreateSessionAsync(Users.Domain.Entities.Session session)
        {
            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();
            return session;
        }


        public async Task<SessionReadDto?> GetSessionByIdAsync(int id)
        {
            var session = await _db.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            return session is null ? null : MapToSessionReadDto(session);
        }


        public async Task<IEnumerable<SessionReadDto>> GetAllSessionsAsync()
        {
            var sessions = await _db.Sessions.Include(s => s.User).ToListAsync();
            return sessions.Select(MapToSessionReadDto).ToList();
        }
        private static SessionReadDto MapToSessionReadDto(Users.Domain.Entities.Session session)
        {
            return new SessionReadDto(
                session.Id,
                session.UserId,
                session.TokenHash,
                session.CreatedAt,
                session.ExpiresAt ?? default,
                session.User != null ? MapToUserReadDto(session.User) : null
            );
        }

        private static UserReadDto MapToUserReadDto(Users.Domain.Entities.User user)
        {
            return new UserReadDto(
                user.Id,
                user.Nickname,
                user.Name,
                user.Lastname,
                user.Email,
                user.Role.ToString(),
                user.Status.ToString(),
                user.EmailConfirmed,
                user.LastLogin,
                user.RegistrationDate,
                user.CreatedAt,
                user.UpdatedAt
            );
        }

        public async Task<Users.Domain.Entities.Session?> UpdateSessionAsync(Users.Domain.Entities.Session session)
        {
            var s = await _db.Sessions.FindAsync(session.Id);
            if (s is null) return null;
            s.ExpiresAt = session.ExpiresAt;
            await _db.SaveChangesAsync();
            return s;
        }

        public async Task<bool> DeleteSessionAsync(int id)
        {
            var s = await _db.Sessions.FindAsync(id);
            if (s is null) return false;
            _db.Sessions.Remove(s);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSessionsByUserIdAsync(int userId)
        {
            var sessions = await _db.Sessions.Where(s => s.UserId == userId).ToListAsync();
            if (!sessions.Any()) return false;
            _db.Sessions.RemoveRange(sessions);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}