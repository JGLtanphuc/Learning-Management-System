using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implementations
{
    public class MockProjectRepository : BaseRepository<MockProject>, IMockProjectRepository
    {
        public MockProjectRepository(AppDbContext context) : base(context)
        {
        }

        public IQueryable<MockProject> QueryWithStudents()
        {
            return _dbSet.Include(m => m.Students);
        }

        public IQueryable<MockProject> QueryWithClass()
        {
            return QueryWithStudents();
        }
    }
}


