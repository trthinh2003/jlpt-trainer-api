using FluentValidation;
using JlptTrainer.Application.Common.ExcelImport;
using JlptTrainer.Application.Common.Helpers;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Vocabs.ImportVocabFromExcel;

public sealed class ImportVocabFromExcelCommandHandler(
    IVocabCatalogWriter writer,
    IApplicationDbContext dbContext,
    IExcelReader excelReader,
    IValidator<CreateVocabCommand> validator) : IRequestHandler<ImportVocabFromExcelCommand, CatalogImportResult>
{
    public async Task<CatalogImportResult> Handle(ImportVocabFromExcelCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.FileContent);
        var rows = excelReader.ReadSheet(stream);
        var keysInWorkbook = new HashSet<(string Word, string Reading)>();
        var errors = new List<CatalogImportRowError>();
        var candidates = new List<Vocab>();

        foreach (var row in rows)
        {
            var levelCell = row.Get("Level");
            if (!JlptLevelParser.TryParse(levelCell, out var level))
            {
                errors.Add(new CatalogImportRowError(row.Number, $"Level \"{levelCell}\" không hợp lệ (chấp nhận N1-N5 hoặc 1-5)."));
                continue;
            }

            var input = new CreateVocabCommand(row.Get("Word") ?? "", row.Get("Reading") ?? "", row.Get("Meaning") ?? "",
                row.Get("ExampleSentence"), row.Get("ExampleSentenceMeaning"), level);
            var validation = await validator.ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                errors.Add(new CatalogImportRowError(row.Number,
                    string.Join(" · ", validation.Errors.Select(error => error.ErrorMessage))));
                continue;
            }

            if (!keysInWorkbook.Add((input.Word, input.Reading)))
            {
                continue;
            }

            candidates.Add(new Vocab
            {
                Word = input.Word,
                Reading = input.Reading,
                Meaning = input.Meaning,
                ExampleSentence = input.ExampleSentence,
                ExampleSentenceMeaning = input.ExampleSentenceMeaning,
                Level = input.Level,
            });
        }

        var acceptedCount = request.PreviewOnly
            ? await CountMissingAsync(keysInWorkbook, cancellationToken)
            : await writer.InsertMissingAsync(candidates, cancellationToken);
        var duplicates = rows.Count - errors.Count - acceptedCount;
        return new CatalogImportResult(rows.Count, acceptedCount, duplicates, errors);
    }

    private async Task<int> CountMissingAsync(HashSet<(string Word, string Reading)> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return 0;
        }

        var words = keys.Select(key => key.Word).Distinct().ToArray();
        var existingKeys = await dbContext.Vocabs
            .Where(vocab => words.Contains(vocab.Word))
            .Select(vocab => new { vocab.Word, vocab.Reading })
            .Distinct()
            .ToListAsync(cancellationToken);

        return keys.Count - existingKeys.Count(key => keys.Contains((key.Word, key.Reading)));
    }
}
