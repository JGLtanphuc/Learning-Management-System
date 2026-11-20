using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IStudentScoreRepository : IRepository<StudentScore>
    {
        Task<StudentScore?> GetWithColumnsByIdAsync(long id);
        IQueryable<StudentScore> QueryWithColumns();
        Task<List<string>> GetAllScoreKeysAsync();
    }
}


