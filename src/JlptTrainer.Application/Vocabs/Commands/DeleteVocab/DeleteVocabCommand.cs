using MediatR;

namespace JlptTrainer.Application.Vocabs.Commands.DeleteVocab
{
    public sealed record DeleteVocabCommand(Guid Id) : IRequest;
}
