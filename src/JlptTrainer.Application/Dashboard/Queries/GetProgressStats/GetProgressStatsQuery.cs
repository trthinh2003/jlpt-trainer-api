using MediatR;

namespace JlptTrainer.Application.Dashboard.Queries.GetProgressStats
{
    public sealed record GetProgressStatsQuery : IRequest<ProgressStatsResult>;
}
