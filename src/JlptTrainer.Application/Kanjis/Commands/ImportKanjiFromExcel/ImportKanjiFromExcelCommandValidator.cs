using FluentValidation;

namespace JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel
{
    public sealed class ImportKanjiFromExcelCommandValidator : AbstractValidator<ImportKanjiFromExcelCommand>
    {
        private const int MaxFileSizeBytes = 5 * 1024 * 1024;

        public ImportKanjiFromExcelCommandValidator()
        {
            RuleFor(x => x.FileContent)
                .NotEmpty().WithMessage("File Excel không được để trống.")
                .Must(content => content.Length <= MaxFileSizeBytes)
                .WithMessage("File Excel không được vượt quá 5MB.");
        }
    }
}
