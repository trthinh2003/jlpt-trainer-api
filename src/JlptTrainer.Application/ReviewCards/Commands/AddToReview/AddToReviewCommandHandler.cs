using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Domain.Entities;
using MediatR;

namespace JlptTrainer.Application.ReviewCards.Commands.AddToReview
{
    public sealed class AddToReviewCommandHandler(
       ICurrentUserService currentUser,
       TimeProvider timeProvider,
       IReviewCardStore reviewCardStore)
       : IRequestHandler<AddToReviewCommand, Guid>
    {
        public async Task<Guid> Handle(AddToReviewCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            var initialState = SrsCalculator.InitialState(timeProvider.GetUtcNow());
            var card = new ReviewCard(userId, request.CardType, request.ReferenceId, initialState);
            var result = await reviewCardStore.AddMissingAsync(userId, request.CardType, [card], cancellationToken);
            if (result.MissingIds.Count > 0)
            {
                throw new NotFoundException(request.CardType.ToString(), request.ReferenceId);
            }

            if (result.AlreadyAddedIds.Count > 0)
            {
                throw new ConflictException("Nội dung này đã có trong bộ ôn tập của bạn rồi.");
            }

            return card.Id;
        }
    }
}
