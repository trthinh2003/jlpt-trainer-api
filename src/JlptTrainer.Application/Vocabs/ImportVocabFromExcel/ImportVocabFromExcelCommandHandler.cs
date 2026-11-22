using FluentValidation;
using JlptTrainer.Application.Common.Helpers;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Domain.Entities;
using MediatR;

namespace JlptTrainer.Application.Vocabs.ImportVocabFromExcel;

public sealed class ImportVocabFromExcelCommandHandler(
    IVocabCatalogWriter writer,
    IExcelReader excelReader,
    IValidator<CreateVocabCommand> validator) : IRequestHandler<ImportVocabFromExcelCommand, ImportVocabResult>
{
    public async Task<ImportVocabResult> Handle(ImportVocabFromExcelCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.FileContent);
        var rows = excelReader.ReadSheet(stream);
        var keysInWorkbook = new HashSet<(string Word, string Reading)>();
        var errors = new List<ImportRowError>();
        var imported = new List<Vocab>();

        foreach (var row in rows)
        {
            var levelCell = row.Get("Level");
            if (!JlptLevelParser.TryParse(levelCell, out var level))
            {
                errors.Add(new ImportRowError(row.Number, $"Level \"{levelCell}\" không hợp lệ (chấp nhận N1-N5 hoặc 1-5)."));
                continue;
            }

            var input = new CreateVocabCommand(row.Get("Word") ?? "", row.Get("Reading") ?? "", row.Get("Meaning") ?? "",
                row.Get("ExampleSentence"), row.Get("ExampleSentenceMeaning"), level);
            var validation = await validator.ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                errors.Add(new ImportRowError(row.Number,
                    string.Join(" · ", validation.Errors.Select(error => error.ErrorMessage))));
                continue;
            }

            if (!keysInWorkbook.Add((input.Word, input.Reading)))
            {
                continue;
            }

            imported.Add(new Vocab
            {
                Word = input.Word,
                Reading = input.Reading,
                Meaning = input.Meaning,
                ExampleSentence = input.ExampleSentence,
                ExampleSentenceMeaning = input.ExampleSentenceMeaning,
                Level = input.Level,
            });
        }

        var successCount = await writer.InsertMissingAsync(imported, cancellationToken);
        var duplicates = rows.Count - errors.Count - successCount;
        return new ImportVocabResult(rows.Count, successCount, duplicates, errors);
    }
}
