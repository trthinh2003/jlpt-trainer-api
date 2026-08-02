using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestHistory
{
    public sealed record MockTestHistoryItemDto(
        Guid Id,
        JlptLevel Level,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        TimeSpan Duration,
        DateTimeOffset TakenAt);
}
