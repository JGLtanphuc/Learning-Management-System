using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implementations
{
    public class StudentScoreRepository : BaseRepository<StudentScore>, IStudentScoreRepository
    {
        public StudentScoreRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<StudentScore?> GetWithColumnsByIdAsync(long id)
        {
            return await _context.StudentScores
                .Include(s => s.StudentScoreColumns)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public IQueryable<StudentScore> QueryWithColumns()
        {
            return _context.StudentScores
                .Include(s => s.StudentScoreColumns)
                .AsQueryable();
        }

        public async Task<List<string>> GetAllScoreKeysAsync()
        {
            return await _context.StudentScoreColumns
                .Select(c => c.ScoreKey)
                .Distinct()
                .ToListAsync();
        }
    }
}


