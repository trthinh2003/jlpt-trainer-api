using JlptTrainer.Application.Vocabs.Queries.GetVocabList;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabById
{
    public sealed record GetVocabByIdQuery(Guid Id) : IRequest<VocabDto>;
}
