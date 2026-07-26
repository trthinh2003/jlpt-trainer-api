namespace JlptTrainer.Domain.ValueObjects
{
    public sealed record SrsResult(
        double EaseFactor,
        int IntervalDays,
        int Repetitions,
        DateTimeOffset NextReviewDate);
}
