using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IMockProjectRepository : IRepository<MockProject>
    {
        IQueryable<MockProject> QueryWithStudents();
        IQueryable<MockProject> QueryWithClass();
    }
}


