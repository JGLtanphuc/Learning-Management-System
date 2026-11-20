using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IAttendanceRepository : IRepository<Attendance>
    {
        IQueryable<Attendance> QueryWithDetails();
    }
}


