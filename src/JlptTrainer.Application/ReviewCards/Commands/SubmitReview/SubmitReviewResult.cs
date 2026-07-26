namespace JlptTrainer.Application.ReviewCards.Commands.SubmitReview
{
    public sealed record SubmitReviewResult(
        Guid ReviewCardId,
        int IntervalDays,
        DateTimeOffset NextReviewDate
    );
}
