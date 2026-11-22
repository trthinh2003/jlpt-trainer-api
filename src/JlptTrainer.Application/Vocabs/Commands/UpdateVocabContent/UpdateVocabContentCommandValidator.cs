using FluentValidation;

using JlptTrainer.Application.Common.Validation;

namespace JlptTrainer.Application.Vocabs.Commands.UpdateVocabContent;

public sealed class UpdateVocabContentCommandValidator : AbstractValidator<UpdateVocabContentCommand>
{
    public UpdateVocabContentCommandValidator()
    {
        RuleFor(x => x.Meaning).ValidMeaning();
    }
}
