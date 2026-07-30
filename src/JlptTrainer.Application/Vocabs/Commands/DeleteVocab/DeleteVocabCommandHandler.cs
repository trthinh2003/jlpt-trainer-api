using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Vocabs.Commands.DeleteVocab
{
    public sealed class DeleteVocabCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<DeleteVocabCommand>
    {
        public async Task Handle(DeleteVocabCommand request, CancellationToken cancellationToken)
        {
            var vocab = await dbContext.Vocabs.FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

            if (vocab is null)
            {
                throw new NotFoundException(nameof(Vocab), request.Id);
            }

            var orphanedCards = await dbContext.ReviewCards
                .Where(c => c.CardType == CardType.Vocab && c.ReferenceId == request.Id)
                .ToListAsync(cancellationToken);

            dbContext.ReviewCards.RemoveRange(orphanedCards);
            dbContext.Vocabs.Remove(vocab);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
