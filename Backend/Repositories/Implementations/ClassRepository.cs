using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implementations
{
    public class ClassRepository : BaseRepository<TrainingClass>, IClassRepository
    {
        public ClassRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<TrainingClass?> GetByCodeAsync(string classCode)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.ClassCode == classCode);
        }

        public IQueryable<TrainingClass> QueryWithStudents()
        {
            return _dbSet.Include(c => c.Students).Include(c => c.Teacher);
        }
    }
}
