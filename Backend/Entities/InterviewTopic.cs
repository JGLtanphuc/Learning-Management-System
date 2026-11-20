using System.Collections.Generic;

namespace Backend.Entities
{
    public class InterviewTopic : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }

        public virtual ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
    }
}
