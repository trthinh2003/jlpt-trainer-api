using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.GrammarPoints.Commands.DeleteGrammarPoint
{
    public sealed class DeleteGrammarPointCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<DeleteGrammarPointCommand>
    {
        public async Task Handle(DeleteGrammarPointCommand request, CancellationToken cancellationToken)
        {
            var grammarPoint = await dbContext.GrammarPoints.FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

            if (grammarPoint is null)
            {
                throw new NotFoundException(nameof(GrammarPoint), request.Id);
            }

            var orphanedCards = await dbContext.ReviewCards
                .Where(c => c.CardType == CardType.Grammar && c.ReferenceId == request.Id)
                .ToListAsync(cancellationToken);

            dbContext.ReviewCards.RemoveRange(orphanedCards);
            dbContext.GrammarPoints.Remove(grammarPoint);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
