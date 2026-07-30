using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabList
{
    public sealed record VocabDto(
        Guid Id,
        string Word,
        string Reading,
        string Meaning,
        string? ExampleSentence,
        string? ExampleSentenceMeaning,
        JlptLevel Level);
}
