using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList
{
    public sealed class GetGrammarPointListQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetGrammarPointListQuery, PagedGrammarPointResult>
    {
        public async Task<PagedGrammarPointResult> Handle(GetGrammarPointListQuery request, CancellationToken cancellationToken)
        {
            var query = dbContext.GrammarPoints.AsQueryable();

            if (request.Level is not null)
            {
                query = query.Where(g => g.Level == request.Level);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(g => g.Pattern)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(g => new GrammarPointDto(g.Id, g.Pattern, g.Meaning, g.ExampleSentence, g.ExampleSentenceMeaning, g.Level))
                .ToListAsync(cancellationToken);

            return new PagedGrammarPointResult(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
