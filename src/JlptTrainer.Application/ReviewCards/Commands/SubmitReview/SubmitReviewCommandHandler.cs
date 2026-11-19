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
            var userId = currentUser.UserId;
            var existing = await FindSubmissionAsync(userId, request, cancellationToken);
            if (existing is not null) return existing;

            var card = await dbContext.ReviewCards.FirstOrDefaultAsync(c => c.Id == request.ReviewCardId, cancellationToken);

            if (card is null)
            {
                throw new NotFoundException(nameof(ReviewCard), request.ReviewCardId);
            }

            if (card.UserId != userId)
            {
                throw new ForbiddenAccessException();
            }

            var now = timeProvider.GetUtcNow();
            // PostgreSQL lưu timestamp đến microsecond. Lần gửi lại cần trả đúng thời điểm ban đầu.
            var reviewedAt = now.AddTicks(-(now.Ticks % 10));

            var result = SrsCalculator.Calculate(
                currentEaseFactor: card.EaseFactor,
                currentRepetitions: card.Repetitions,
                currentIntervalDays: card.IntervalDays,
                grade: request.Grade,
                reviewedAt: reviewedAt);

            card.ApplyReview(result, reviewedAt);
            dbContext.ReviewEvents.Add(new ReviewEvent(card, request.Grade, reviewedAt, request.RequestId));

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                var completed = await FindSubmissionAsync(userId, request, cancellationToken);
                if (completed is not null) return completed;
                if (exception is DbUpdateConcurrencyException)
                {
                    throw new ConflictException("Thẻ vừa thay đổi. Vui lòng tải lại danh sách ôn tập.");
                }
                throw;
            }

            return new SubmitReviewResult(card.Id, card.IntervalDays, card.NextReviewDate);
        }

        private async Task<SubmitReviewResult?> FindSubmissionAsync(Guid userId, SubmitReviewCommand request, CancellationToken cancellationToken)
        {
            if (request.RequestId is not Guid requestId) return null;

            var review = await dbContext.ReviewEvents.AsNoTracking().SingleOrDefaultAsync(
                e => e.UserId == userId && e.RequestId == requestId, cancellationToken);
            if (review is null) return null;
            if (review.ReviewCardId != request.ReviewCardId || review.Grade != request.Grade)
            {
                throw new ConflictException("Mã yêu cầu đã được dùng cho một lượt ôn khác.");
            }

            return new SubmitReviewResult(review.ReviewCardId, review.IntervalDays, review.NextReviewDate);
        }
    }
}
