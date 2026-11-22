using FluentValidation;

using JlptTrainer.Application.Common.Validation;

namespace JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint
{
    public sealed class CreateGrammarPointCommandValidator : AbstractValidator<CreateGrammarPointCommand>
    {
        public CreateGrammarPointCommandValidator()
        {
            RuleFor(x => x.Pattern).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Meaning).ValidMeaning();
            RuleFor(x => x.Level).IsInEnum();
        }
    }
}
