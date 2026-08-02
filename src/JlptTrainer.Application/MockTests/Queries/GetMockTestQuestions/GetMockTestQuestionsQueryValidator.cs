using FluentValidation;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions
{
    public sealed class GetMockTestQuestionsQueryValidator : AbstractValidator<GetMockTestQuestionsQuery>
    {
        public GetMockTestQuestionsQueryValidator()
        {
            RuleFor(x => x.Level).IsInEnum();
            RuleFor(x => x.QuestionCount).InclusiveBetween(1, 50)
                .WithMessage("Số câu hỏi phải trong khoảng 1-50.");
        }
    }
}
