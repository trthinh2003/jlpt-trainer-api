using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Domain.ValueObjects;

namespace JlptTrainer.Domain.Entities
{
    public class ReviewCard : BaseEntity
    {
        private ReviewCard() { }

        public ReviewCard(Guid userId, CardType cardType, Guid referenceId, SrsResult initialState)
        {
            UserId = userId;
            CardType = cardType;
            ReferenceId = referenceId;
            SetSchedule(initialState);
        }

        public Guid UserId { get; private set; }

        public CardType CardType { get; private set; }

        // Tham chiếu tới Vocab, Kanji hoặc GrammarPoint tùy CardType.
        public Guid ReferenceId { get; private set; }

        public double EaseFactor { get; private set; }

        public int IntervalDays { get; private set; }

        public int Repetitions { get; private set; }

        public DateTimeOffset NextReviewDate { get; private set; }

        public DateTimeOffset? LastReviewedAt { get; private set; }

        public bool IsDue(DateTimeOffset now) => NextReviewDate <= now;

        public void ApplyReview(SrsResult result, DateTimeOffset reviewedAt)
        {
            SetSchedule(result);
            LastReviewedAt = reviewedAt;
        }

        private void SetSchedule(SrsResult result)
        {
            EaseFactor = result.EaseFactor;
            IntervalDays = result.IntervalDays;
            Repetitions = result.Repetitions;
            NextReviewDate = result.NextReviewDate;
        }
    }
}
