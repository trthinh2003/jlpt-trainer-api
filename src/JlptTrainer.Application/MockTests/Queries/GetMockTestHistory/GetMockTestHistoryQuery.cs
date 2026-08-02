using MediatR;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestHistory
{
    public sealed record GetMockTestHistoryQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedMockTestHistoryResult>;
}
