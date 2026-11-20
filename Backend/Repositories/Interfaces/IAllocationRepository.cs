using Backend.Data;
using Backend.Entities;

namespace Backend.Repositories.Interfaces
{
    public interface IAllocationRepository : IRepository<Allocation>
    {
        IQueryable<Allocation> QueryWithStudent();
    }
}


