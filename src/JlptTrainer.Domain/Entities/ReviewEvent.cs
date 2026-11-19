using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities;

public sealed class ReviewEvent
{
    private ReviewEvent() { }

    public ReviewEvent(ReviewCard card, ReviewGrade grade, DateTimeOffset reviewedAt, Guid? requestId)
    {
        UserId = card.UserId;
        ReviewCardId = card.Id;
        Grade = grade;
        ReviewedAt = reviewedAt;
        RequestId = requestId;
        IntervalDays = card.IntervalDays;
        NextReviewDate = card.NextReviewDate;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid ReviewCardId { get; private set; }
    public ReviewGrade? Grade { get; private set; }
    public DateTimeOffset ReviewedAt { get; private set; }
    public Guid? RequestId { get; private set; }
    public int IntervalDays { get; private set; }
    public DateTimeOffset NextReviewDate { get; private set; }
}
