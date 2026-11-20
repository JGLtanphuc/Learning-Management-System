using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;

namespace Backend.Repositories.Implementations
{
    public class InterviewQuestionRepository : BaseRepository<InterviewQuestion>, IInterviewQuestionRepository
    {
        public InterviewQuestionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
