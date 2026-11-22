using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabList
{
    public sealed class GetVocabListQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetVocabListQuery, PagedVocabResult>
    {
        public async Task<PagedVocabResult> Handle(GetVocabListQuery request, CancellationToken cancellationToken)
        {
            var query = dbContext.Vocabs.AsQueryable();

            if (request.Level is not null)
            {
                query = query.Where(v => v.Level == request.Level);
            }

            var search = request.Search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.Word.ToLower().Contains(search)
                    || v.Reading.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var offset = (request.PageNumber - 1L) * request.PageSize;
            if (offset >= totalCount)
            {
                return new PagedVocabResult([], totalCount, request.PageNumber, request.PageSize);
            }

            var items = await query
                .OrderBy(v => v.Word)
                .ThenBy(v => v.Reading)
                .ThenBy(v => v.Id)
                .Skip((int)offset)
                .Take(request.PageSize)
                .Select(v => new VocabDto(
                    v.Id,
                    v.Word,
                    v.Reading,
                    v.Meaning,
                    v.ExampleSentence,
                    v.ExampleSentenceMeaning,
                    v.Level))
                .ToListAsync(cancellationToken);

            return new PagedVocabResult(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
