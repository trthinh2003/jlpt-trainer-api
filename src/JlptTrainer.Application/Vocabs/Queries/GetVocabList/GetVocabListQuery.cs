using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabList
{
    public sealed record GetVocabListQuery(
        JlptLevel? Level = null,
        int PageNumber = 1,
        int PageSize = 20) : IRequest<PagedVocabResult>;
}
