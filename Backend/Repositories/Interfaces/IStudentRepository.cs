using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IStudentRepository : IRepository<Student>
    {
        Task<Student?> GetByCodeAsync(string studentCode);
        Task<Student?> GetByEmailAsync(string email);
        IQueryable<Student> QueryWithClass();
    }
}


