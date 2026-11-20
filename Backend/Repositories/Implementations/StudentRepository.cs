using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implementations
{
    public class StudentRepository : BaseRepository<Student>, IStudentRepository
    {
        public StudentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Student?> GetByCodeAsync(string studentCode)
        {
            return await _dbSet.Include(s => s.Class).Include(s => s.MockProject).FirstOrDefaultAsync(s => s.StudentCode == studentCode);
        }

        public async Task<Student?> GetByEmailAsync(string email)
        {
            return await _dbSet.Include(s => s.Class).Include(s => s.MockProject).FirstOrDefaultAsync(s => s.Email == email);
        }

        public IQueryable<Student> QueryWithClass()
        {
            return _dbSet.Include(s => s.Class).Include(s => s.MockProject);
        }
    }
}


