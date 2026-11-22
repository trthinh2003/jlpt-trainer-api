using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.ReviewCards.Commands.AddReviewCardsBatch;

public sealed record AddReviewCardsBatchCommand(CardType CardType, Guid[] ReferenceIds) : IRequest<ReviewCardAddition>
{
    public const int MaxCards = 20;
}
