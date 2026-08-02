using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestHistory
{
    public sealed class GetMockTestHistoryQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRequestHandler<GetMockTestHistoryQuery, PagedMockTestHistoryResult>
    {
        public async Task<PagedMockTestHistoryResult> Handle(GetMockTestHistoryQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;

            var query = dbContext.MockTests.Where(m => m.UserId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(m => m.TakenAt) // bài mới nhất lên đầu
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new MockTestHistoryItemDto(
                    m.Id,
                    m.Level,
                    m.TotalQuestions,
                    m.CorrectAnswers,
                    m.TotalQuestions == 0 ? 0 : Math.Round((double)m.CorrectAnswers / m.TotalQuestions * 100, 2),
                    m.Duration,
                    m.TakenAt))
                .ToListAsync(cancellationToken);

            return new PagedMockTestHistoryResult(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
