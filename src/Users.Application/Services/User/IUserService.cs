using Users.Application.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Users.Application.Services.User
{
    public interface IUserService
    {
        Task<Users.Domain.Entities.User> CreateUserAsync(Users.Domain.Entities.User user);
        Task<Users.Domain.Entities.User?> GetUserByIdAsync(int id);
        Task<IEnumerable<Users.Domain.Entities.User>> GetAllUsersAsync();
        Task<Users.Domain.Entities.User?> UpdateUserAsync(Users.Domain.Entities.User user);
        Task<Users.Domain.Entities.User?> UpdateUserAsync(int id, UserPatchDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<Users.Domain.Entities.User?> AuthenticateAsync(string email, string password);
        Task<bool> DeleteAllDataAsync();
    }
}