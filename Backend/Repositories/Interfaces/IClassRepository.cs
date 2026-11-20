using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IClassRepository : IRepository<TrainingClass>
    {
        Task<TrainingClass?> GetByCodeAsync(string classCode);
        IQueryable<TrainingClass> QueryWithStudents();
    }
}


