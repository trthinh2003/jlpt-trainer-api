using FluentValidation;
using JlptTrainer.Application.Common.Validation;

namespace JlptTrainer.Application.Kanjis.Commands.UpdateKanjiContent;

public sealed class UpdateKanjiContentCommandValidator : AbstractValidator<UpdateKanjiContentCommand>
{
    public UpdateKanjiContentCommandValidator()
    {
        RuleFor(x => x.Meaning).ValidMeaning();
        RuleFor(x => x.OnYomi).MaximumLength(200);
        RuleFor(x => x.KunYomi).MaximumLength(200);
    }
}
