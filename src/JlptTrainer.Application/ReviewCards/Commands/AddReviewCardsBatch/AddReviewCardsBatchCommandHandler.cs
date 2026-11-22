using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Domain.Entities;
using MediatR;

namespace JlptTrainer.Application.ReviewCards.Commands.AddReviewCardsBatch;

public sealed class AddReviewCardsBatchCommandHandler(
    IReviewCardStore store,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<AddReviewCardsBatchCommand, ReviewCardAddition>
{
    public Task<ReviewCardAddition> Handle(AddReviewCardsBatchCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var initialState = SrsCalculator.InitialState(timeProvider.GetUtcNow());
        var cards = request.ReferenceIds.Distinct()
            .Select(id => new ReviewCard(userId, request.CardType, id, initialState))
            .ToArray();
        return store.AddMissingAsync(userId, request.CardType, cards, cancellationToken);
    }
}
