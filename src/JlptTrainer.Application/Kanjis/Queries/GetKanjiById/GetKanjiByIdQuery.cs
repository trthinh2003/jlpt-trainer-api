using JlptTrainer.Application.Kanjis.Queries.GetKanjiList;
using MediatR;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiById
{
    public sealed record GetKanjiByIdQuery(Guid Id) : IRequest<KanjiDto>;
}
