using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.ReviewCards;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.Kanjis.Commands.DeleteKanji
{
    public sealed class DeleteKanjiCommandHandler(IReviewCardStore store)
       : IRequestHandler<DeleteKanjiCommand>
    {
        public async Task Handle(DeleteKanjiCommand request, CancellationToken cancellationToken)
        {
            if (!await store.DeleteContentAsync(CardType.Kanji, request.Id, cancellationToken))
            {
                throw new NotFoundException(nameof(Kanji), request.Id);
            }
        }
    }
}
