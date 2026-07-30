using FluentValidation;

namespace JlptTrainer.Application.ReviewCards.Commands.AddToReview
{
    public sealed class AddToReviewCommandValidator : AbstractValidator<AddToReviewCommand>
    {
        public AddToReviewCommandValidator()
        {
            RuleFor(x => x.CardType).IsInEnum();
            RuleFor(x => x.ReferenceId).NotEmpty();
        }
    }
}
