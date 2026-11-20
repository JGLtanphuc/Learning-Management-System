using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;

namespace Backend.Repositories.Implementations
{
    public class InterviewAnswerRepository : BaseRepository<InterviewAnswer>, IInterviewAnswerRepository
    {
        public InterviewAnswerRepository(AppDbContext context) : base(context)
        {
        }
    }
}
