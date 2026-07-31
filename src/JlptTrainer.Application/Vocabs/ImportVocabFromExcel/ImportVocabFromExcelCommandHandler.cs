using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Vocabs.ImportVocabFromExcel
{
    public sealed class ImportVocabFromExcelCommandHandler(IApplicationDbContext dbContext, IExcelReader excelReader) : IRequestHandler<ImportVocabFromExcelCommand, ImportVocabResult>
    {
        public async Task<ImportVocabResult> Handle(ImportVocabFromExcelCommand request, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(request.FileContent);
            var rows = excelReader.ReadSheet(stream);

            var existingKeys = await dbContext.Vocabs 
                .Select(v => new { v.Word, v.Reading })
                .ToListAsync(cancellationToken);

            var existingSet = existingKeys
                .Select(k => (k.Word, k.Reading))
                .ToHashSet();

            var errors = new List<ImportRowError>();
            var newVocabs = new List<Vocab>();
            var seenInFile = new HashSet<(string Word, string Reading)>();
            var skippedDuplicateCount = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 2; // +2 vì dòng 1 là header, Excel đếm từ 1
                var row = rows[i];

                var word = GetValue(row, "Word");
                var reading = GetValue(row, "Reading");
                var meaning = GetValue(row, "Meaning");
                var levelRaw = GetValue(row, "Level");

                if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(reading) || string.IsNullOrWhiteSpace(meaning))
                {
                    errors.Add(new ImportRowError(rowNumber, "Thiếu Word/Reading/Meaning bắt buộc."));
                    continue;
                }

                if (!TryParseLevel(levelRaw, out var level))
                {
                    errors.Add(new ImportRowError(
                        rowNumber, $"Level \"{levelRaw}\" không hợp lệ (chấp nhận N1-N5 hoặc 1-5)."));
                    continue;
                }

                var key = (word.Trim(), reading.Trim());

                // trùng với DB đã có, hoặc trùng với 1 dòng khác đứng trước trong cùng file Excel
                if (existingSet.Contains(key) || !seenInFile.Add(key))
                {
                    skippedDuplicateCount++;
                    continue;
                }

                newVocabs.Add(new Vocab
                {
                    Word = key.Item1,
                    Reading = key.Item2,
                    Meaning = meaning.Trim(),
                    ExampleSentence = GetValue(row, "ExampleSentence")?.Trim(),
                    ExampleSentenceMeaning = GetValue(row, "ExampleSentenceMeaning")?.Trim(),
                    Level = level
                });
            }

            if (newVocabs.Count > 0)
            {
                dbContext.Vocabs.AddRange(newVocabs);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new ImportVocabResult(
                TotalRows: rows.Count,
                SuccessCount: newVocabs.Count,
                SkippedDuplicateCount: skippedDuplicateCount,
                Errors: errors);
        }

        private static string? GetValue(IReadOnlyDictionary<string, string?> row, string key) => row.TryGetValue(key, out var value) ? value : null;

        private static bool TryParseLevel(string? raw, out JlptLevel level)
        {
            level = JlptLevel.N5;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return true; // để trống -> mặc định N5
            }

            var trimmed = raw.Trim().ToUpperInvariant();

            if (trimmed.StartsWith('N') && Enum.TryParse<JlptLevel>(trimmed, out level)) // chấp nhận cả 2 format: "N5" (chữ) hay "5" (số) - cái này tùy người tạo file Excel quen kiểu nào
            {
                return true;
            }

            if (int.TryParse(trimmed, out var num) && Enum.IsDefined(typeof(JlptLevel), num))
            {
                level = (JlptLevel)num;
                return true;
            }

            return false;
        }
    }
}
