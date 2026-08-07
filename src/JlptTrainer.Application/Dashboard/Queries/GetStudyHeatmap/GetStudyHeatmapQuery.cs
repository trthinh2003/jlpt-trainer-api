using MediatR;

namespace JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap
{
    public sealed record GetStudyHeatmapQuery(int Days = 365) : IRequest<List<HeatmapEntryDto>>;
}
