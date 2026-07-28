using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiList
{
    public sealed class GetKanjiListQueryHandler(IApplicationDbContext dbContext)
        : IRequestHandler<GetKanjiListQuery, PagedKanjiResult>
    {
        public async Task<PagedKanjiResult> Handle(
            GetKanjiListQuery request,
            CancellationToken cancellationToken)
        {
            var query = dbContext.Kanjis.AsQueryable();

            if (request.Level is not null)
            {
                query = query.Where(k => k.Level == request.Level);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(k => k.StrokeCount) // học kanji ít nét trước là thứ tự tự nhiên nhất
                .ThenBy(k => k.Character)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(k => new KanjiDto(
                    k.Id, k.Character, k.OnYomi, k.KunYomi, k.Meaning, k.StrokeCount, k.Level))
                .ToListAsync(cancellationToken);

            return new PagedKanjiResult(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
