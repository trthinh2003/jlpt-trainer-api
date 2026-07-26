using FluentValidation;

namespace JlptTrainer.Application.ReviewCards.Commands.SubmitReview
{
    public sealed class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
    {
        public SubmitReviewCommandValidator()
        {
            RuleFor(x => x.ReviewCardId)
                .NotEmpty();

            RuleFor(x => x.Grade)
                .IsInEnum();
        }
    }
}
