using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiList
{
    public sealed record KanjiDto(
        Guid Id,
        string Character,
        string OnYomi,
        string KunYomi,
        string Meaning,
        int StrokeCount,
        JlptLevel Level);
}
