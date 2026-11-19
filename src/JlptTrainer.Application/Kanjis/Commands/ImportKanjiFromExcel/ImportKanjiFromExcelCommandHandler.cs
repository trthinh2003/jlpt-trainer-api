using FluentValidation;
using JlptTrainer.Application.Common.Helpers;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Kanjis.Commands.CreateKanji;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel;

public sealed class ImportKanjiFromExcelCommandHandler(
    IApplicationDbContext dbContext,
    IExcelReader excelReader,
    IValidator<CreateKanjiCommand> validator) : IRequestHandler<ImportKanjiFromExcelCommand, ImportKanjiResult>
{
    public async Task<ImportKanjiResult> Handle(ImportKanjiFromExcelCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.FileContent);
        var rows = excelReader.ReadSheet(stream);
        var keys = await dbContext.Kanjis.Select(item => item.Character).ToListAsync(cancellationToken);
        var knownKeys = keys.ToHashSet();
        var errors = new List<ImportKanjiRowError>();
        var imported = new List<Kanji>();
        var duplicates = 0;

        foreach (var row in rows)
        {
            var levelCell = row.Get("Level");
            if (!JlptLevelParser.TryParse(levelCell, out var level))
            {
                errors.Add(new ImportKanjiRowError(row.Number, $"Level \"{levelCell}\" không hợp lệ (chấp nhận N1-N5 hoặc 1-5)."));
                continue;
            }

            if (!int.TryParse(row.Get("StrokeCount"), out var strokeCount))
            {
                errors.Add(new ImportKanjiRowError(row.Number, "StrokeCount phải là số nguyên."));
                continue;
            }

            var input = new CreateKanjiCommand(row.Get("Character") ?? "", row.Get("OnYomi") ?? "", row.Get("KunYomi") ?? "",
                row.Get("Meaning") ?? "", strokeCount, level);
            var validation = await validator.ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                errors.Add(new ImportKanjiRowError(row.Number,
                    string.Join(" · ", validation.Errors.Select(error => error.ErrorMessage))));
                continue;
            }

            if (!knownKeys.Add(input.Character))
            {
                duplicates++;
                continue;
            }

            imported.Add(new Kanji
            {
                Character = input.Character,
                OnYomi = input.OnYomi,
                KunYomi = input.KunYomi,
                Meaning = input.Meaning,
                StrokeCount = input.StrokeCount,
                Level = input.Level,
            });
        }

        if (imported.Count > 0)
        {
            dbContext.Kanjis.AddRange(imported);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ImportKanjiResult(rows.Count, imported.Count, duplicates, errors);
    }
}
