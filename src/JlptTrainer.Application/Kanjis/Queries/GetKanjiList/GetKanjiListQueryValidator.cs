using FluentValidation;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiList
{
    public sealed class GetKanjiListQueryValidator : AbstractValidator<GetKanjiListQuery>
    {
        public GetKanjiListQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
