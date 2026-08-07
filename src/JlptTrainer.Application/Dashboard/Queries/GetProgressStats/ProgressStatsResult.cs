namespace JlptTrainer.Application.Dashboard.Queries.GetProgressStats
{
    public sealed record ProgressStatsResult(
        List<CardTypeProgressDto> CardProgress, 
        List<MockTestTrendDto> MockTestTrend);
}
