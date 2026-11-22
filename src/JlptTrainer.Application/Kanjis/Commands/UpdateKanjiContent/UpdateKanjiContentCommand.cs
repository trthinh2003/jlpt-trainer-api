using MediatR;

namespace JlptTrainer.Application.Kanjis.Commands.UpdateKanjiContent;

public sealed record UpdateKanjiContentCommand(
    Guid Id,
    string Meaning,
    string? OnYomi,
    string? KunYomi) : IRequest<Unit>;
