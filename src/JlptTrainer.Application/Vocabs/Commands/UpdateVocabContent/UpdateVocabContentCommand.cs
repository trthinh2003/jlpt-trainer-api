using MediatR;

namespace JlptTrainer.Application.Vocabs.Commands.UpdateVocabContent;

public sealed record UpdateVocabContentCommand(
    Guid Id,
    string Meaning,
    string? ExampleSentence,
    string? ExampleSentenceMeaning) : IRequest<Unit>;
