using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.ReviewCards.Commands.AddToReview
{
    public sealed class AddToReviewCommandHandler(
       IApplicationDbContext dbContext,
       ICurrentUserService currentUser,
       TimeProvider timeProvider)
       : IRequestHandler<AddToReviewCommand, Guid>
    {
        public async Task<Guid> Handle(AddToReviewCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;

            var contentExists = request.CardType switch
            {
                CardType.Vocab => await dbContext.Vocabs.AnyAsync(v => v.Id == request.ReferenceId, cancellationToken),
                CardType.Kanji => await dbContext.Kanjis.AnyAsync(k => k.Id == request.ReferenceId, cancellationToken),
                CardType.Grammar => await dbContext.GrammarPoints.AnyAsync(g => g.Id == request.ReferenceId, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(request.CardType))
            };

            if (!contentExists)
            {
                throw new NotFoundException(request.CardType.ToString(), request.ReferenceId);
            }

            var alreadyAdded = await dbContext.ReviewCards.AnyAsync(
                c => c.UserId == userId && c.CardType == request.CardType && c.ReferenceId == request.ReferenceId,
                cancellationToken);

            if (alreadyAdded)
            {
                throw new ConflictException("Nội dung này đã có trong bộ ôn tập của bạn rồi.");
            }

            var now = timeProvider.GetUtcNow();
            var initialState = SrsCalculator.InitialState(now);

            var card = new ReviewCard
            {
                UserId = userId,
                CardType = request.CardType,
                ReferenceId = request.ReferenceId,
                EaseFactor = initialState.EaseFactor,
                IntervalDays = initialState.IntervalDays,
                Repetitions = initialState.Repetitions,
                NextReviewDate = initialState.NextReviewDate
            };

            dbContext.ReviewCards.Add(card);
            await dbContext.SaveChangesAsync(cancellationToken);

            return card.Id;
        }
    }
}
