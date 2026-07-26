using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities
{
    public class MockTest : BaseEntity
    {
        public Guid UserId { get; set; }

        public JlptLevel Level { get; set; } = JlptLevel.N5;

        public int TotalQuestions { get; set; }

        public int CorrectAnswers { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTimeOffset TakenAt { get; set; } = DateTimeOffset.UtcNow;

        public double ScorePercentage => TotalQuestions == 0 ? 0 : Math.Round((double)CorrectAnswers / TotalQuestions * 100, 2);
    }
}
