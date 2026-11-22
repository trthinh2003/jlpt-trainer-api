using FluentValidation;

namespace JlptTrainer.Application.ReviewCards.Commands.AddReviewCardsBatch;

public sealed class AddReviewCardsBatchCommandValidator : AbstractValidator<AddReviewCardsBatchCommand>
{
    public AddReviewCardsBatchCommandValidator()
    {
        RuleFor(x => x.CardType).IsInEnum();
        RuleFor(x => x.ReferenceIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Chọn ít nhất một mục học liệu.")
            .Must(ids => ids.Length <= AddReviewCardsBatchCommand.MaxCards)
            .WithMessage($"Chỉ có thể thêm tối đa {AddReviewCardsBatchCommand.MaxCards} mục mỗi lần.");
        RuleForEach(x => x.ReferenceIds).NotEmpty();
    }
}
