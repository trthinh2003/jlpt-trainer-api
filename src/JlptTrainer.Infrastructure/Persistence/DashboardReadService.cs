using Dapper;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Dashboard.Queries.GetProgressStats;
using JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Infrastructure.Persistence;

internal sealed class DashboardReadService(DapperContext dapperContext) : IDashboardReadService
{
    private const string HeatmapSql = """
        SELECT CAST(study_date AS timestamp) AS StudyDate, review_count AS ReviewCount
        FROM get_study_heatmap(@UserId, @FromUtc)
        WHERE study_date < CAST(@UntilUtc AT TIME ZONE 'UTC' AS date)
        ORDER BY study_date;
        """;

    private const string CardProgressSql = """
        SELECT card_type AS CardType, total_cards AS TotalCards, mastered_count AS MasteredCount
        FROM get_card_progress(@UserId, @MasteryThreshold)
        ORDER BY card_type;
        """;

    private const string MockTestTrendSql = """
        SELECT CAST(taken_date AS timestamp) AS TakenDate, level AS Level,
               average_score AS AverageScore, test_count AS TestCount
        FROM get_mock_test_trend(@UserId)
        ORDER BY taken_date, level;
        """;

    public async Task<List<HeatmapEntryDto>> GetHeatmapAsync(
        Guid userId, DateTimeOffset fromUtc, DateTimeOffset untilUtc, CancellationToken cancellationToken)
    {
        using var connection = dapperContext.CreateConnection();
        var rows = await connection.QueryAsync<HeatmapRow>(new CommandDefinition(
            HeatmapSql, new { UserId = userId, FromUtc = fromUtc, UntilUtc = untilUtc }, cancellationToken: cancellationToken));

        return rows.Select(row => new HeatmapEntryDto(DateOnly.FromDateTime(row.StudyDate), row.ReviewCount)).ToList();
    }

    public async Task<ProgressStatsResult> GetProgressAsync(Guid userId, int masteryThreshold, CancellationToken cancellationToken)
    {
        using var connection = dapperContext.CreateConnection();
        var cards = await connection.QueryAsync<CardProgressRow>(new CommandDefinition(
            CardProgressSql, new { UserId = userId, MasteryThreshold = masteryThreshold }, cancellationToken: cancellationToken));
        var trend = await connection.QueryAsync<MockTestTrendRow>(new CommandDefinition(
            MockTestTrendSql, new { UserId = userId }, cancellationToken: cancellationToken));

        return new ProgressStatsResult(
            cards.Select(row => new CardTypeProgressDto((CardType)row.CardType, row.TotalCards, row.MasteredCount)).ToList(),
            trend.Select(row => new MockTestTrendDto(
                DateOnly.FromDateTime(row.TakenDate), (JlptLevel)row.Level, (double)row.AverageScore, row.TestCount)).ToList());
    }

    private sealed record HeatmapRow(DateTime StudyDate, long ReviewCount);
    private sealed record CardProgressRow(int CardType, long TotalCards, long MasteredCount);
    private sealed record MockTestTrendRow(DateTime TakenDate, int Level, decimal AverageScore, long TestCount);
}
