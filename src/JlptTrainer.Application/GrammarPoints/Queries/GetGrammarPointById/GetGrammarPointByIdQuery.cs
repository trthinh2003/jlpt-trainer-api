using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList;
using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointById
{
    public sealed record GetGrammarPointByIdQuery(Guid Id) : IRequest<GrammarPointDto>;
}

