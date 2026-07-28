using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiList
{
    public sealed record GetKanjiListQuery(
        JlptLevel? Level = null,
        int PageNumber = 1,
        int PageSize = 20) : IRequest<PagedKanjiResult>;
}
