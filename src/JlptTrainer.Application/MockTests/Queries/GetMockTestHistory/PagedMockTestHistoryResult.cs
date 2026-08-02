namespace JlptTrainer.Application.MockTests.Queries.GetMockTestHistory
{
    public sealed record PagedMockTestHistoryResult(List<MockTestHistoryItemDto> Items, int TotalCount, int PageNumber, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
