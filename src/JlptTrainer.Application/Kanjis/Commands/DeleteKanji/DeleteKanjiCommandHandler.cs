using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JlptTrainer.Application.Kanjis.Commands.DeleteKanji
{
    public sealed class DeleteKanjiCommandHandler(IApplicationDbContext dbContext)
       : IRequestHandler<DeleteKanjiCommand>
    {
        public async Task Handle(DeleteKanjiCommand request, CancellationToken cancellationToken)
        {
            var kanji = await dbContext.Kanjis.FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken);

            if (kanji is null)
            {
                throw new NotFoundException(nameof(Kanji), request.Id);
            }

            var orphanedCards = await dbContext.ReviewCards
                .Where(c => c.CardType == CardType.Kanji && c.ReferenceId == request.Id)
                .ToListAsync(cancellationToken);

            dbContext.ReviewCards.RemoveRange(orphanedCards);
            dbContext.Kanjis.Remove(kanji);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
