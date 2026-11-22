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

            var search = request.Search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(k => k.Character.ToLower().Contains(search)
                    || k.OnYomi.ToLower().Contains(search)
                    || k.KunYomi.ToLower().Contains(search)
                    || k.Meaning.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var offset = (request.PageNumber - 1L) * request.PageSize;
            if (offset >= totalCount)
            {
                return new PagedKanjiResult([], totalCount, request.PageNumber, request.PageSize);
            }

            var items = await query
                .OrderBy(k => k.StrokeCount) // học kanji ít nét trước là thứ tự tự nhiên nhất
                .ThenBy(k => k.Character)
                .Skip((int)offset)
                .Take(request.PageSize)
                .Select(k => new KanjiDto(
                    k.Id, k.Character, k.OnYomi, k.KunYomi, k.Meaning, k.StrokeCount, k.Level))
                .ToListAsync(cancellationToken);

            return new PagedKanjiResult(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
