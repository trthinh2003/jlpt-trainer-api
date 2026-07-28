using FluentValidation;

namespace JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint
{
    public sealed class CreateGrammarPointCommandValidator : AbstractValidator<CreateGrammarPointCommand>
    {
        public CreateGrammarPointCommandValidator()
        {
            RuleFor(x => x.Pattern).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Meaning).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Level).IsInEnum();
        }
    }
}
