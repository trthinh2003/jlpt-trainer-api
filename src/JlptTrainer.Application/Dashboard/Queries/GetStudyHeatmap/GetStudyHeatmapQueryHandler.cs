using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap
{
    public sealed class GetStudyHeatmapQueryHandler(
        IDashboardReadService dashboard,
        ICurrentUserService currentUser,
        TimeProvider timeProvider
    ) : IRequestHandler<GetStudyHeatmapQuery, List<HeatmapEntryDto>>
    {
        public Task<List<HeatmapEntryDto>> Handle(GetStudyHeatmapQuery request, CancellationToken cancellationToken)
        {
            var today = new DateTimeOffset(timeProvider.GetUtcNow().UtcDateTime.Date, TimeSpan.Zero);
            return dashboard.GetHeatmapAsync(currentUser.UserId, today.AddDays(1 - request.Days), today.AddDays(1), cancellationToken);
        }
    }
}
