using JlptTrainer.Application.Common.Interfaces;
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

            return await dbContext.ReviewCards
                .Where(c => c.UserId == userId && c.NextReviewDate <= now)
                .OrderBy(c => c.NextReviewDate) // thẻ trễ hạn lâu nhất ưu tiên ôn trước
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
