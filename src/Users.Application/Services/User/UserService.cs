using Users.Application;
using Users.Application.Dtos;
using System.Threading.Tasks;
using Users.Infrastructure.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Users.Application.Services.User
{
    public sealed class UserService : IUserService
    {
        private readonly UsersDbContext _db;
        private readonly IPasswordService _passwordService;

        public async Task<Users.Domain.Entities.User?> AuthenticateAsync(string email, string password)
        {
            var user = await _db.Users
                .Include(u => u.Preference)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return null;
            }

            if (!_passwordService.Verify(password, user.Password))
            {
                return null;
            }

            return user;
        }

        public UserService(UsersDbContext db, IPasswordService passwordService)
        {
            _db = db;
            _passwordService = passwordService;
        }

        public async Task<Users.Domain.Entities.User> CreateUserAsync(Users.Domain.Entities.User user)
        {
            if (await _db.Users.AnyAsync(u => u.Email == user.Email))
            {
                throw new InvalidOperationException(Localization.Get("Error_EmailExists"));
            }

            if (await _db.Users.AnyAsync(u => u.Nickname == user.Nickname))
            {
                throw new InvalidOperationException(Localization.Get("Error_NicknameExists"));
            }

            user.Password = _passwordService.Hash(user.Password);
            user.Role = Users.Domain.Entities.UserRole.user;
            user.Status = Users.Domain.Entities.UserStatus.active;
            user.EmailConfirmed = false;
            var now = DateTime.UtcNow;
            user.RegistrationDate = now;
            user.CreatedAt = now;
            user.UpdatedAt = now;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<Users.Domain.Entities.User?> GetUserByIdAsync(int id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task<IEnumerable<Users.Domain.Entities.User>> GetAllUsersAsync()
        {
            return await _db.Users.ToListAsync();
        }

        public async Task<Users.Domain.Entities.User?> UpdateUserAsync(Users.Domain.Entities.User user)
        {
            var u = await _db.Users.FindAsync(user.Id);
            if (u is null)
            {
                return null;
            }

            // Validar email único si se quiere actualizar
            if (!string.IsNullOrEmpty(user.Email) && user.Email != u.Email)
            {
                var exists = await _db.Users.AnyAsync(x => x.Email == user.Email && x.Id != u.Id);
                if (exists)
                {
                    throw new InvalidOperationException(Localization.Get("Error_EmailExists"));
                }

                u.Email = user.Email;
            }

            if (!string.IsNullOrEmpty(user.Password))
            {
                u.Password = _passwordService.Hash(user.Password);
            }

            if (!string.IsNullOrEmpty(user.Nickname))
            {
                u.Nickname = user.Nickname;
            }

            if (!string.IsNullOrEmpty(user.Name))
            {
                u.Name = user.Name;
            }

            if (!string.IsNullOrEmpty(user.Lastname))
            {
                u.Lastname = user.Lastname;
            }

            u.Role = user.Role;
            u.Status = user.Status;
            u.EmailConfirmed = user.EmailConfirmed;
            u.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return u;
        }

        public async Task<Users.Domain.Entities.User?> UpdateUserAsync(int id, UserPatchDto dto)
        {
            var u = await _db.Users.FindAsync(id);
            if (u is null)
            {
                return null;
            }

            await ValidateAndUpdateEmailAsync(u, dto.Email);
            await ValidateAndUpdateNicknameAsync(u, dto.Nickname);

            if (!string.IsNullOrEmpty(dto.Password))
            {
                u.Password = _passwordService.Hash(dto.Password);
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                u.Name = dto.Name;
            }

            if (!string.IsNullOrEmpty(dto.Lastname))
            {
                u.Lastname = dto.Lastname;
            }

            if (!string.IsNullOrEmpty(dto.Role) && Enum.TryParse<Users.Domain.Entities.UserRole>(dto.Role, out var role))
            {
                u.Role = role;
            }

            if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<Users.Domain.Entities.UserStatus>(dto.Status, out var status))
            {
                u.Status = status;
            }

            if (dto.EmailConfirmed.HasValue)
            {
                u.EmailConfirmed = dto.EmailConfirmed.Value;
            }

            u.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return u;
        }

        private async Task ValidateAndUpdateEmailAsync(Users.Domain.Entities.User user, string? newEmail)
        {
            if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email)
            {
                var exists = await _db.Users.AnyAsync(x => x.Email == newEmail && x.Id != user.Id);
                if (exists)
                {
                    throw new InvalidOperationException(Localization.Get("Error_EmailExists"));
                }

                user.Email = newEmail;
            }
        }

        private async Task ValidateAndUpdateNicknameAsync(Users.Domain.Entities.User user, string? newNickname)
        {
            if (!string.IsNullOrEmpty(newNickname) && newNickname != user.Nickname)
            {
                var exists = await _db.Users.AnyAsync(x => x.Nickname == newNickname && x.Id != user.Id);
                if (exists)
                {
                    throw new InvalidOperationException(Localization.Get("Error_NicknameExists"));
                }

                user.Nickname = newNickname;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u is null)
            {
                return false;
            }

            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllDataAsync()
        {
            try
            {
                // Usar Entity Framework en lugar de SQL directo para compatibilidad con InMemory DB

                // 1. Eliminar sessions
                var sessions = await _db.Sessions.ToListAsync();
                if (sessions.Any())
                {
                    _db.Sessions.RemoveRange(sessions);
                }

                // 2. Eliminar preferences
                var preferences = await _db.Preferences.ToListAsync();
                if (preferences.Any())
                {
                    _db.Preferences.RemoveRange(preferences);
                }

                // 3. Eliminar users
                var users = await _db.Users.ToListAsync();
                if (users.Any())
                {
                    _db.Users.RemoveRange(users);
                }

                await _db.SaveChangesAsync();

                // Reset auto increment IDs solo para bases de datos relacionales (no InMemory)
                // Verificar de manera más robusta si es InMemory database
                var isInMemoryDb = _db.Database.ProviderName?.Contains("InMemory") == true;

                if (!isInMemoryDb)
                {
                    try
                    {
                        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE SESSIONS AUTO_INCREMENT = 1");
                        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE PREFERENCES AUTO_INCREMENT = 1");
                        await _db.Database.ExecuteSqlRawAsync("ALTER TABLE USERS AUTO_INCREMENT = 1");
                    }
                    catch (Exception)
                    {
                        // Ignorar errores de ALTER TABLE en caso de bases de datos que no soporten AUTO_INCREMENT
                        // Esto no debe afectar la funcionalidad principal de eliminación de datos
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}