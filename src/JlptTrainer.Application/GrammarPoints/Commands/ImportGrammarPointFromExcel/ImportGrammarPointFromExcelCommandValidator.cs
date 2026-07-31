using FluentValidation;

namespace JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel
{
    public sealed class ImportGrammarPointFromExcelCommandValidator : AbstractValidator<ImportGrammarPointFromExcelCommand>
    {
        private const int MaxFileSizeBytes = 5 * 1024 * 1024;

        public ImportGrammarPointFromExcelCommandValidator()
        {
            RuleFor(x => x.FileContent)
                .NotEmpty().WithMessage("File Excel không được để trống.")
                .Must(content => content.Length <= MaxFileSizeBytes)
                .WithMessage("File Excel không được vượt quá 5MB.");
        }
    }
}
