using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.ReviewCards.Commands.SubmitReview
{
    public sealed record SubmitReviewCommand(
        Guid ReviewCardId,
        ReviewGrade Grade
    ) : IRequest<SubmitReviewResult>;
}
