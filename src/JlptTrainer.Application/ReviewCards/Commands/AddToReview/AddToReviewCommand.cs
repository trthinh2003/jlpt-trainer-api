using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.ReviewCards.Commands.AddToReview
{
    public sealed record AddToReviewCommand(
        CardType CardType,
        Guid ReferenceId) : IRequest<Guid>;
}
