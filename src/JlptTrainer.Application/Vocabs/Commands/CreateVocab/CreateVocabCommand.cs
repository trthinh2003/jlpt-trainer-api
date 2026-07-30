using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Commands.CreateVocab
{
    public sealed record CreateVocabCommand(
        string Word,
        string Reading,
        string Meaning,
        string? ExampleSentence,
        string? ExampleSentenceMeaning,
        JlptLevel Level) : IRequest<Guid>;
}
