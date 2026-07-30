namespace JlptTrainer.Application.Vocabs.Queries.GetVocabList
{
    public sealed record PagedVocabResult(
       List<VocabDto> Items,
       int TotalCount,
       int PageNumber,
       int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
