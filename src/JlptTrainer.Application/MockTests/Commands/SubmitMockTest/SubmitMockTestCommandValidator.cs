using FluentValidation;

namespace JlptTrainer.Application.MockTests.Commands.SubmitMockTest
{
    public sealed class SubmitMockTestCommandValidator : AbstractValidator<SubmitMockTestCommand>
    {
        public SubmitMockTestCommandValidator()
        {
            RuleFor(x => x.Level).IsInEnum();

            RuleFor(x => x.Answers).NotEmpty().WithMessage("Bài thi phải có ít nhất 1 câu trả lời.");

            RuleForEach(x => x.Answers).ChildRules(answer =>
            {
                answer.RuleFor(a => a.VocabId).NotEmpty();
                answer.RuleFor(a => a.SelectedMeaning).NotEmpty();
            });

            RuleFor(x => x.Duration)
                .GreaterThan(TimeSpan.Zero)
                .WithMessage("Thời gian làm bài phải lớn hơn 0.");
        }
    }
}
