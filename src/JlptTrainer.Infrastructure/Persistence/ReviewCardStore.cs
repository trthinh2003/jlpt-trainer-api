using JlptTrainer.Application.ReviewCards;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JlptTrainer.Infrastructure.Persistence;

public sealed class ReviewCardStore(ApplicationDbContext dbContext) : IReviewCardStore
{
    public async Task<ReviewCardAddition> AddMissingAsync(
        Guid userId, CardType cardType, IReadOnlyList<ReviewCard> cards, CancellationToken cancellationToken)
    {
        var requestedIds = cards.Select(card => card.ReferenceId).ToArray();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var presentIds = await LockContentAsync(cardType, requestedIds, cancellationToken);
        var present = presentIds.ToHashSet();
        var alreadyAdded = (await dbContext.ReviewCards
            .Where(card => card.UserId == userId && card.CardType == cardType && presentIds.Contains(card.ReferenceId))
            .Select(card => card.ReferenceId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var newCards = cards.Where(card => present.Contains(card.ReferenceId) && !alreadyAdded.Contains(card.ReferenceId)).ToArray();

        dbContext.ReviewCards.AddRange(newCards);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ReviewCardAddition(
            newCards.Select(card => card.ReferenceId).ToArray(),
            requestedIds.Where(alreadyAdded.Contains).ToArray(),
            requestedIds.Where(id => !present.Contains(id)).ToArray());
    }

    public async Task<bool> DeleteContentAsync(CardType cardType, Guid referenceId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        if ((await LockContentAsync(cardType, [referenceId], cancellationToken)).Count == 0) return false;

        await dbContext.ReviewCards.Where(card => card.CardType == cardType && card.ReferenceId == referenceId)
            .ExecuteDeleteAsync(cancellationToken);
        await (cardType switch
        {
            CardType.Vocab => dbContext.Vocabs.Where(item => item.Id == referenceId).ExecuteDeleteAsync(cancellationToken),
            CardType.Kanji => dbContext.Kanjis.Where(item => item.Id == referenceId).ExecuteDeleteAsync(cancellationToken),
            CardType.Grammar => dbContext.GrammarPoints.Where(item => item.Id == referenceId).ExecuteDeleteAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(cardType))
        });
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    // Single adds, batch adds and deletes serialize on the same catalog rows.
    // A consistent lock order lets overlapping batches complete without a lock cycle.
    private Task<List<Guid>> LockContentAsync(CardType cardType, Guid[] ids, CancellationToken cancellationToken)
    {
        // Only use literal table names. The ID array is passed as a SQL parameter.
        FormattableString query = cardType switch
        {
            CardType.Vocab => $"SELECT id AS \"Value\" FROM vocabs WHERE id = ANY({ids}) ORDER BY id FOR UPDATE",
            CardType.Kanji => $"SELECT id AS \"Value\" FROM kanjis WHERE id = ANY({ids}) ORDER BY id FOR UPDATE",
            CardType.Grammar => $"SELECT id AS \"Value\" FROM grammar_points WHERE id = ANY({ids}) ORDER BY id FOR UPDATE",
            _ => throw new ArgumentOutOfRangeException(nameof(cardType))
        };
        return dbContext.Database.SqlQuery<Guid>(query).ToListAsync(cancellationToken);
    }
}
