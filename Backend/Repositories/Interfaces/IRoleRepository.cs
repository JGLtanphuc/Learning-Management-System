using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetByIdWithPermissionsAsync(long id);
        IQueryable<Role> QueryWithPermissions();
    }
}


