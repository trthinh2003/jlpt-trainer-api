using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Domain.ValueObjects;

namespace JlptTrainer.Domain.Entities
{
    public class ReviewCard : BaseEntity
    {
        public Guid UserId { get; set; }

        public CardType CardType { get; set; }

        public Guid ReferenceId { get; set; } /// trỏ tới Vocab.Id, Kanji.Id hay GrammarPoint.Id tùy theo CardType

        // ----- SM-2 -----
        public double EaseFactor { get; set; } = 2.5;  /// hệ số dễ nhớ, mặc định 2.5, không bao giờ nhỏ hơn 1.3 (theo công thức gốc SM-2)

        public int IntervalDays { get; set; } = 0; /// số ngày tới lần ôn tiếp theo
       
        public int Repetitions { get; set; } = 0; /// số lần ôn đúng liên tiếp, reset về 0 khi chọn Again

        public DateTimeOffset NextReviewDate { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LastReviewedAt { get; set; }

        public bool IsDue(DateTimeOffset now) => NextReviewDate <= now;

        public void ApplyReview(SrsResult result, DateTimeOffset reviewedAt)
        {
            EaseFactor = result.EaseFactor;
            IntervalDays = result.IntervalDays;
            Repetitions = result.Repetitions;
            NextReviewDate = result.NextReviewDate;
            LastReviewedAt = reviewedAt;
        }
    }
}
