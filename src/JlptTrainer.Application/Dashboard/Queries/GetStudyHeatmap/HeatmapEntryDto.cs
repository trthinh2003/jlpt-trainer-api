namespace JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap
{
    public sealed record HeatmapEntryDto(DateOnly StudyDate, int ReviewCount);
}
