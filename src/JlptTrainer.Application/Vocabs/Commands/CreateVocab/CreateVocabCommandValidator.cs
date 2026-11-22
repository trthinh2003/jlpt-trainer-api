using FluentValidation;

using JlptTrainer.Application.Common.Validation;

namespace JlptTrainer.Application.Vocabs.Commands.CreateVocab
{
    public sealed class CreateVocabCommandValidator : AbstractValidator<CreateVocabCommand>
    {
        public CreateVocabCommandValidator()
        {
            RuleFor(x => x.Word).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Reading).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Meaning).ValidMeaning();
            RuleFor(x => x.Level).IsInEnum();
        }
    }
}
