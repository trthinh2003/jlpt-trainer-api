using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Vocabs.Commands.GetVocabList;
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

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(v => v.Word) 
                .Skip((request.PageNumber - 1) * request.PageSize)
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
