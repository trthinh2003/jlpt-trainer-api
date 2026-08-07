using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.Dashboard.Queries.GetProgressStats
{
    public sealed record MockTestTrendDto(DateOnly TakenDate, JlptLevel Level, double AverageScore, int TestCount);
}
