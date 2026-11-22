using FluentValidation;
using JlptTrainer.Application.Common.Validation;

namespace JlptTrainer.Application.GrammarPoints.Commands.UpdateGrammarPointContent;

public sealed class UpdateGrammarPointContentCommandValidator : AbstractValidator<UpdateGrammarPointContentCommand>
{
    public UpdateGrammarPointContentCommandValidator()
    {
        RuleFor(x => x.Meaning).ValidMeaning();
    }
}
