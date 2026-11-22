using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Kanjis.Commands.UpdateKanjiContent;

public sealed class UpdateKanjiContentCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateKanjiContentCommand, Unit>
{
    public async Task<Unit> Handle(UpdateKanjiContentCommand request, CancellationToken cancellationToken)
    {
        var meaning = request.Meaning.Trim();
        var onYomi = request.OnYomi?.Trim() ?? string.Empty;
        var kunYomi = request.KunYomi?.Trim() ?? string.Empty;

        var updated = await dbContext.Kanjis
            .Where(item => item.Id == request.Id)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.Meaning, meaning)
                .SetProperty(item => item.OnYomi, onYomi)
                .SetProperty(item => item.KunYomi, kunYomi), cancellationToken);

        if (updated == 0)
        {
            throw new NotFoundException(nameof(Kanji), request.Id);
        }

        return Unit.Value;
    }
}
