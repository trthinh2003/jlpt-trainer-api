using FluentValidation;

namespace JlptTrainer.Application.ReviewCards.Queries.GetDueCards;

public sealed class GetDueCardsQueryValidator : AbstractValidator<GetDueCardsQuery>
{
    public GetDueCardsQueryValidator()
    {
        RuleFor(x => x.CardType).IsInEnum();
        RuleFor(x => x.Level).IsInEnum();
    }
}
