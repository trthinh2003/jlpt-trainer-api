using JlptTrainer.Application.Common.Helpers;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel
{
    public sealed class ImportKanjiFromExcelCommandHandler(IApplicationDbContext dbContext, IExcelReader excelReader) : IRequestHandler<ImportKanjiFromExcelCommand, ImportKanjiResult>
    {
        public async Task<ImportKanjiResult> Handle(ImportKanjiFromExcelCommand request, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(request.FileContent);
            var rows = excelReader.ReadSheet(stream);

            var existingCharacters = await dbContext.Kanjis
                .Select(k => k.Character)
                .ToHashSetAsync(cancellationToken);

            var errors = new List<ImportKanjiRowError>();
            var newKanjis = new List<Kanji>();
            var seenInFile = new HashSet<string>();
            var skippedDuplicateCount = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 2;
                var row = rows[i];

                var character = GetValue(row, "Character");
                var meaning = GetValue(row, "Meaning");
                var strokeCountRaw = GetValue(row, "StrokeCount");
                var levelRaw = GetValue(row, "Level");

                if (string.IsNullOrWhiteSpace(character) || string.IsNullOrWhiteSpace(meaning))
                {
                    errors.Add(new ImportKanjiRowError(rowNumber, "Thiếu Character/Meaning bắt buộc."));
                    continue;
                }

                if (!int.TryParse(strokeCountRaw, out var strokeCount) || strokeCount <= 0)
                {
                    errors.Add(new ImportKanjiRowError(rowNumber, $"StrokeCount \"{strokeCountRaw}\" không hợp lệ (phải là số nguyên dương)."));
                    continue;
                }

                if (!JlptLevelParser.TryParse(levelRaw, out var level))
                {
                    errors.Add(new ImportKanjiRowError( rowNumber, $"Level \"{levelRaw}\" không hợp lệ (chấp nhận N1-N5 hoặc 1-5)."));
                    continue;
                }

                var trimmedCharacter = character.Trim();

                if (existingCharacters.Contains(trimmedCharacter) || !seenInFile.Add(trimmedCharacter))
                {
                    skippedDuplicateCount++;
                    continue;
                }

                newKanjis.Add(new Kanji
                {
                    Character = trimmedCharacter,
                    OnYomi = GetValue(row, "OnYomi")?.Trim() ?? string.Empty,
                    KunYomi = GetValue(row, "KunYomi")?.Trim() ?? string.Empty,
                    Meaning = meaning.Trim(),
                    StrokeCount = strokeCount,
                    Level = level
                });
            }

            if (newKanjis.Count > 0)
            {
                dbContext.Kanjis.AddRange(newKanjis);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new ImportKanjiResult(rows.Count, newKanjis.Count, skippedDuplicateCount, errors);
        }

        private static string? GetValue(IReadOnlyDictionary<string, string?> row, string key) => row.TryGetValue(key, out var value) ? value : null;
    }
}
