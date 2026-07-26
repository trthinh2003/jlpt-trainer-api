using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.ReviewCards.Commands.SubmitReview
{
    public sealed class SubmitReviewCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        TimeProvider timeProvider
    ) : IRequestHandler<SubmitReviewCommand, SubmitReviewResult>
    {
        public async Task<SubmitReviewResult> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
        {
            var card = await dbContext.ReviewCards.FirstOrDefaultAsync(c => c.Id == request.ReviewCardId, cancellationToken);

            if (card is null)
            {
                throw new NotFoundException(nameof(ReviewCard), request.ReviewCardId);
            }
     
            if (card.UserId != currentUser.UserId) // chặn user A ôn thẻ của user B - mỗi ReviewCard gắn cứng với 1 UserId
            {
                throw new ForbiddenAccessException();
            }

            var now = timeProvider.GetUtcNow();

            var result = SrsCalculator.Calculate(
                currentEaseFactor: card.EaseFactor,
                currentRepetitions: card.Repetitions,
                currentIntervalDays: card.IntervalDays,
                grade: request.Grade,
                reviewedAt: now);

            card.ApplyReview(result, now);

            await dbContext.SaveChangesAsync(cancellationToken);

            return new SubmitReviewResult(card.Id, card.IntervalDays, card.NextReviewDate);
        }
    }
}
