using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.ReviewCards;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Commands.DeleteGrammarPoint
{
    public sealed class DeleteGrammarPointCommandHandler(IReviewCardStore store) : IRequestHandler<DeleteGrammarPointCommand>
    {
        public async Task Handle(DeleteGrammarPointCommand request, CancellationToken cancellationToken)
        {
            if (!await store.DeleteContentAsync(CardType.Grammar, request.Id, cancellationToken))
            {
                throw new NotFoundException(nameof(GrammarPoint), request.Id);
            }

        }
    }
}
