using FluentValidation;
using JlptTrainer.Application.Common.Helpers;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel;

public sealed class ImportGrammarPointFromExcelCommandHandler(
    IApplicationDbContext dbContext,
    IExcelReader excelReader,
    IValidator<CreateGrammarPointCommand> validator) : IRequestHandler<ImportGrammarPointFromExcelCommand, ImportGrammarPointResult>
{
    public async Task<ImportGrammarPointResult> Handle(ImportGrammarPointFromExcelCommand request, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(request.FileContent);
        var rows = excelReader.ReadSheet(stream);
        var keys = await dbContext.GrammarPoints.Select(item => item.Pattern).ToListAsync(cancellationToken);
        var knownKeys = keys.ToHashSet();
        var errors = new List<ImportGrammarPointRowError>();
        var imported = new List<GrammarPoint>();
        var duplicates = 0;

        foreach (var row in rows)
        {
            var levelCell = row.Get("Level");
            if (!JlptLevelParser.TryParse(levelCell, out var level))
            {
                errors.Add(new ImportGrammarPointRowError(row.Number, $"Level \"{levelCell}\" không hợp lệ (chấp nhận N1-N5 hoặc 1-5)."));
                continue;
            }

            var input = new CreateGrammarPointCommand(row.Get("Pattern") ?? "", row.Get("Meaning") ?? "",
                row.Get("ExampleSentence"), row.Get("ExampleSentenceMeaning"), level);
            var validation = await validator.ValidateAsync(input, cancellationToken);
            if (!validation.IsValid)
            {
                errors.Add(new ImportGrammarPointRowError(row.Number,
                    string.Join(" · ", validation.Errors.Select(error => error.ErrorMessage))));
                continue;
            }

            if (!knownKeys.Add(input.Pattern))
            {
                duplicates++;
                continue;
            }

            imported.Add(new GrammarPoint
            {
                Pattern = input.Pattern,
                Meaning = input.Meaning,
                ExampleSentence = input.ExampleSentence,
                ExampleSentenceMeaning = input.ExampleSentenceMeaning,
                Level = input.Level,
            });
        }

        if (imported.Count > 0)
        {
            dbContext.GrammarPoints.AddRange(imported);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ImportGrammarPointResult(rows.Count, imported.Count, duplicates, errors);
    }
}
