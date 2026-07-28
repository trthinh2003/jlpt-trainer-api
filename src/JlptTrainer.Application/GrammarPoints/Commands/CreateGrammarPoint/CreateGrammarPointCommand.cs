using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint
{
    public sealed record CreateGrammarPointCommand(
        string Pattern,
        string Meaning,
        string? ExampleSentence,
        string? ExampleSentenceMeaning,
        JlptLevel Level) : IRequest<Guid>;
}
