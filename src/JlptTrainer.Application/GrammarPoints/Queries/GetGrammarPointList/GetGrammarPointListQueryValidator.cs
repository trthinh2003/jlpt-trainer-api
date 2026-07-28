using FluentValidation;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList
{
    public sealed class GetGrammarPointListQueryValidator : AbstractValidator<GetGrammarPointListQuery>
    {
        public GetGrammarPointListQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
