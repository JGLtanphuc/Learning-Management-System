using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;

namespace Backend.Repositories.Implementations
{
    public class InterviewTopicRepository : BaseRepository<InterviewTopic>, IInterviewTopicRepository
    {
        public InterviewTopicRepository(AppDbContext context) : base(context)
        {
        }
    }
}
