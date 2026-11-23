using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.ReviewCards.Queries.GetDueCards
{
    public sealed class GetDueCardsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
        : IRequestHandler<GetDueCardsQuery, List<DueCardDto>>
    {
        public async Task<List<DueCardDto>> Handle(
            GetDueCardsQuery request,
            CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow();
            var userId = currentUser.UserId;

            var dueCards = dbContext.ReviewCards.Where(c => c.UserId == userId && c.NextReviewDate <= now);
            if (request.CardType is { } type)
            {
                dueCards = dueCards.Where(c => c.CardType == type);
            }

            if (request.Level is { } level)
            {
                dueCards = dueCards.Where(c =>
                    (c.CardType == CardType.Vocab && dbContext.Vocabs.Any(v => v.Id == c.ReferenceId && v.Level == level)) ||
                    (c.CardType == CardType.Kanji && dbContext.Kanjis.Any(k => k.Id == c.ReferenceId && k.Level == level)) ||
                    (c.CardType == CardType.Grammar && dbContext.GrammarPoints.Any(g => g.Id == c.ReferenceId && g.Level == level)));
            }

            return await dueCards
                .OrderBy(c => c.NextReviewDate) // thẻ trễ hạn lâu nhất ưu tiên ôn trước
                .ThenBy(c => c.Id)
                .Take(request.MaxCards)
                .Select(c => new DueCardDto(
                    c.Id,
                    c.CardType,
                    c.ReferenceId,
                    c.Repetitions,
                    c.NextReviewDate))
                .ToListAsync(cancellationToken);
        }
    }
}
