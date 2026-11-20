using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;

namespace Backend.Repositories.Implementations
{
    public class InterviewAssignmentRepository : BaseRepository<InterviewAssignment>, IInterviewAssignmentRepository
    {
        public InterviewAssignmentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
