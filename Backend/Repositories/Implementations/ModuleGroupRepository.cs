using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;

namespace Backend.Repositories.Implementations
{
    public class ModuleGroupRepository : BaseRepository<ModuleGroup>, IModuleGroupRepository
    {
        public ModuleGroupRepository(AppDbContext context) : base(context)
        {
        }
    }
}


