using JlptTrainer.Application.Vocabs;
using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Infrastructure.Persistence;

public sealed class VocabCatalogWriter(ApplicationDbContext dbContext) : IVocabCatalogWriter
{
    public Task<int> InsertMissingAsync(IReadOnlyList<Vocab> vocabs, CancellationToken cancellationToken)
    {
        if (vocabs.Count == 0)
        {
            return Task.FromResult(0);
        }

        var ids = vocabs.Select(vocab => vocab.Id).ToArray();
        var words = vocabs.Select(vocab => vocab.Word).ToArray();
        var readings = vocabs.Select(vocab => vocab.Reading).ToArray();
        var meanings = vocabs.Select(vocab => vocab.Meaning).ToArray();
        var examples = vocabs.Select(vocab => vocab.ExampleSentence).ToArray();
        var exampleMeanings = vocabs.Select(vocab => vocab.ExampleSentenceMeaning).ToArray();
        var levels = vocabs.Select(vocab => (int)vocab.Level).ToArray();
        var createdAt = vocabs.Select(vocab => vocab.CreatedAt).ToArray();

        // Acquire conflicting index entries in the same order for overlapping imports.
        return dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO vocabs (id, word, reading, meaning, example_sentence, example_sentence_meaning, level, created_at)
            SELECT entry.id, entry.word, entry.reading, entry.meaning,
                   entry.example_sentence, entry.example_sentence_meaning, entry.level, entry.created_at
            FROM unnest({ids}::uuid[], {words}::text[], {readings}::text[], {meanings}::text[],
                        {examples}::text[], {exampleMeanings}::text[], {levels}::integer[], {createdAt}::timestamptz[])
                AS entry(id, word, reading, meaning, example_sentence, example_sentence_meaning, level, created_at)
            ORDER BY entry.word, entry.reading
            ON CONFLICT (word, reading) DO NOTHING
            """, cancellationToken);
    }
}
