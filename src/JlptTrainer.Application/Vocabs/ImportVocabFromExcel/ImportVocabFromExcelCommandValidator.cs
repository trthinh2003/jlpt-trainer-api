using FluentValidation;

namespace JlptTrainer.Application.Vocabs.ImportVocabFromExcel
{
    public sealed class ImportVocabFromExcelCommandValidator : AbstractValidator<ImportVocabFromExcelCommand>
    {
        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public ImportVocabFromExcelCommandValidator()
        {
            RuleFor(x => x.FileContent)
                .NotEmpty().WithMessage("File Excel không được để trống.")
                .Must(content => content.Length <= MaxFileSizeBytes)
                .WithMessage("File Excel không được vượt quá 5MB.");
        }
    }
}
