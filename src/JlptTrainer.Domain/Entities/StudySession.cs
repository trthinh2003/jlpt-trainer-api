using JlptTrainer.Domain.Common;

namespace JlptTrainer.Domain.Entities
{
    public class StudySession : BaseEntity
    {
        public Guid UserId { get; set; }

        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? EndedAt { get; set; }

        public int CardsReviewed { get; set; }

        public int CorrectCount { get; set; }
    }
}
