using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.Dashboard.Queries.GetProgressStats
{
    public sealed class GetProgressStatsQueryHandler(
      IDashboardReadService dashboard,
      ICurrentUserService currentUser)
      : IRequestHandler<GetProgressStatsQuery, ProgressStatsResult>
    {
        private const int MasteryThreshold = 3;

        public Task<ProgressStatsResult> Handle(
            GetProgressStatsQuery request,
            CancellationToken cancellationToken)
            => dashboard.GetProgressAsync(currentUser.UserId, MasteryThreshold, cancellationToken);
    }
}
