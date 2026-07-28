namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList
{
    public sealed record PagedGrammarPointResult(
        List<GrammarPointDto> Items,
        int TotalCount,
        int PageNumber,
        int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
