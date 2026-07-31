using JlptTrainer.Application.Common.Helpers;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel
{
    public sealed class ImportGrammarPointFromExcelCommandHandler(IApplicationDbContext dbContext, IExcelReader excelReader) : IRequestHandler<ImportGrammarPointFromExcelCommand, ImportGrammarPointResult>
    {
        public async Task<ImportGrammarPointResult> Handle(ImportGrammarPointFromExcelCommand request, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(request.FileContent);
            var rows = excelReader.ReadSheet(stream);

            var existingPatterns = await dbContext.GrammarPoints
                .Select(g => g.Pattern)
                .ToHashSetAsync(cancellationToken);

            var errors = new List<ImportGrammarPointRowError>();
            var newGrammarPoints = new List<GrammarPoint>();
            var seenInFile = new HashSet<string>();
            var skippedDuplicateCount = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 2;
                var row = rows[i];

                var pattern = GetValue(row, "Pattern");
                var meaning = GetValue(row, "Meaning");
                var levelRaw = GetValue(row, "Level");

                if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(meaning))
                {
                    errors.Add(new ImportGrammarPointRowError(rowNumber, "Thiếu Pattern/Meaning bắt buộc."));
                    continue;
                }

                if (!JlptLevelParser.TryParse(levelRaw, out var level))
                {
                    errors.Add(new ImportGrammarPointRowError(rowNumber, $"Level \"{levelRaw}\" không hợp lệ (chấp nhận N1-N5 hoặc 1-5)."));
                    continue;
                }

                var trimmedPattern = pattern.Trim();

                if (existingPatterns.Contains(trimmedPattern) || !seenInFile.Add(trimmedPattern))
                {
                    skippedDuplicateCount++;
                    continue;
                }

                newGrammarPoints.Add(new GrammarPoint
                {
                    Pattern = trimmedPattern,
                    Meaning = meaning.Trim(),
                    ExampleSentence = GetValue(row, "ExampleSentence")?.Trim(),
                    ExampleSentenceMeaning = GetValue(row, "ExampleSentenceMeaning")?.Trim(),
                    Level = level
                });
            }

            if (newGrammarPoints.Count > 0)
            {
                dbContext.GrammarPoints.AddRange(newGrammarPoints);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new ImportGrammarPointResult(rows.Count, newGrammarPoints.Count, skippedDuplicateCount, errors);
        }

        private static string? GetValue(IReadOnlyDictionary<string, string?> row, string key) => row.TryGetValue(key, out var value) ? value : null;
    }
}
