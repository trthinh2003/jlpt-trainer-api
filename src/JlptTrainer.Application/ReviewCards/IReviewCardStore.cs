using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.ReviewCards;

public interface IReviewCardStore
{
    Task<ReviewCardAddition> AddMissingAsync(
        Guid userId, CardType cardType, IReadOnlyList<ReviewCard> cards, CancellationToken cancellationToken);

    Task<bool> DeleteContentAsync(CardType cardType, Guid referenceId, CancellationToken cancellationToken);
}

public sealed record ReviewCardAddition(
    IReadOnlyList<Guid> AddedIds,
    IReadOnlyList<Guid> AlreadyAddedIds,
    IReadOnlyList<Guid> MissingIds);
