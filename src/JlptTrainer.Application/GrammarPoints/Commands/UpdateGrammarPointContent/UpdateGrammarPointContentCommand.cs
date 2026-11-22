using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Commands.UpdateGrammarPointContent;

public sealed record UpdateGrammarPointContentCommand(
    Guid Id,
    string Meaning,
    string? ExampleSentence,
    string? ExampleSentenceMeaning) : IRequest<Unit>;
