using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList
{
    public sealed record GetGrammarPointListQuery(
        JlptLevel? Level = null,
        int PageNumber = 1,
        int PageSize = 20) : IRequest<PagedGrammarPointResult>;
}
