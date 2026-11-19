using JlptTrainer.Application.Dashboard.Queries.GetProgressStats;
using JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap;

namespace JlptTrainer.Application.Common.Interfaces;

public interface IDashboardReadService
{
    Task<List<HeatmapEntryDto>> GetHeatmapAsync(
        Guid userId, DateTimeOffset fromUtc, DateTimeOffset untilUtc, CancellationToken cancellationToken);

    Task<ProgressStatsResult> GetProgressAsync(Guid userId, int masteryThreshold, CancellationToken cancellationToken);
}
