using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.ReviewCards.Queries.GetDueCards
{
    public sealed record GetDueCardsQuery(int MaxCards = 20) : IRequest<List<DueCardDto>>;

    public sealed record DueCardDto(
        Guid ReviewCardId,
        CardType CardType,
        Guid ReferenceId,
        int Repetitions,
        DateTimeOffset NextReviewDate
    );
}
