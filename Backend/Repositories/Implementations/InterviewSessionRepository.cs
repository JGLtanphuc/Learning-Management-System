using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;

namespace Backend.Repositories.Implementations
{
    public class InterviewSessionRepository : BaseRepository<InterviewSession>, IInterviewSessionRepository
    {
        public InterviewSessionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
