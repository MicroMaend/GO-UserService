using GOCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IUserDBService
    {
        Task<User> CreateUserAsync(User user);
        Task<User> GetUserByIdAsync(Guid userId);
        Task<User> GetUserByNameAsync(string name);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> UpdateUserAsync(string id, User updatedUser);
        Task<bool> DeleteUserAsync(string id);
    }
}
