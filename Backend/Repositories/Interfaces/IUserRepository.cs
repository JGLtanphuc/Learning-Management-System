using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailWithRolesAsync(string email);
        Task<User?> GetByIdWithRolesAsync(long id);
        IQueryable<User> QueryWithRoles();
    }
}


