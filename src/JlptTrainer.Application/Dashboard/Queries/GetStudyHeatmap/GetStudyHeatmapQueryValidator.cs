using FluentValidation;

namespace JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap
{
    public sealed class GetStudyHeatmapQueryValidator : AbstractValidator<GetStudyHeatmapQuery>
    {
        public GetStudyHeatmapQueryValidator()
        {
            RuleFor(x => x.Days).InclusiveBetween(1, 730)
                .WithMessage("Days phải trong khoảng 1-730 (tối đa 2 năm).");
        }
    }
}
