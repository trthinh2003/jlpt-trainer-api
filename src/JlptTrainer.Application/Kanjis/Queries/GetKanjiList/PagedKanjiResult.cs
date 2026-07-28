namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiList
{
    public sealed record PagedKanjiResult(
        List<KanjiDto> Items,
        int TotalCount,
        int PageNumber,
        int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
