using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implementations
{
    public class AllocationRepository : BaseRepository<Allocation>, IAllocationRepository
    {
        public AllocationRepository(AppDbContext context) : base(context)
        {
        }

        public IQueryable<Allocation> QueryWithStudent()
        {
            return _dbSet.Include(a => a.Student).ThenInclude(s => s.Class);
        }
    }
}
