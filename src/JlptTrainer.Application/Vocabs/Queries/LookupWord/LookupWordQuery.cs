using MediatR;

namespace JlptTrainer.Application.Vocabs.Queries.LookupWord
{
    public sealed record LookupWordQuery(string Keyword) : IRequest<List<LookupWordResultDto>>;
}
