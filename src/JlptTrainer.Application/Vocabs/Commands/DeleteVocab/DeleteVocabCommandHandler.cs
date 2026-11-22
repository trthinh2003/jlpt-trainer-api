using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.ReviewCards;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Commands.DeleteVocab
{
    public sealed class DeleteVocabCommandHandler(IReviewCardStore store) : IRequestHandler<DeleteVocabCommand>
    {
        public async Task Handle(DeleteVocabCommand request, CancellationToken cancellationToken)
        {
            if (!await store.DeleteContentAsync(CardType.Vocab, request.Id, cancellationToken))
            {
                throw new NotFoundException(nameof(Vocab), request.Id);
            }

        }
    }
}
