using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.Kanjis.Commands.CreateKanji
{
    public sealed record CreateKanjiCommand(
        string Character,
        string OnYomi,
        string KunYomi,
        string Meaning,
        int StrokeCount,
        JlptLevel Level) : IRequest<Guid>;
}
