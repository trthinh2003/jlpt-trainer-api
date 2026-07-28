using MediatR;

namespace JlptTrainer.Application.Kanjis.Commands.DeleteKanji
{
    public sealed record DeleteKanjiCommand(Guid Id) : IRequest;
}
