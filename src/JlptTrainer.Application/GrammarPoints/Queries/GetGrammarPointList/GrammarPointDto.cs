using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList
{
    public sealed record GrammarPointDto(
        Guid Id,
        string Pattern,
        string Meaning,
        string? ExampleSentence,
        string? ExampleSentenceMeaning,
        JlptLevel Level);
}
