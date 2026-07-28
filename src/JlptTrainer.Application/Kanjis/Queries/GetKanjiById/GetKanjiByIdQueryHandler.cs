using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiList;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiById
{
    public sealed class GetKanjiByIdQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetKanjiByIdQuery, KanjiDto>
    {
        public async Task<KanjiDto> Handle(GetKanjiByIdQuery request, CancellationToken cancellationToken)
        {
            var kanji = await dbContext.Kanjis
                .Where(k => k.Id == request.Id)
                .Select(k => new KanjiDto(k.Id, k.Character, k.OnYomi, k.KunYomi, k.Meaning, k.StrokeCount, k.Level))
                .FirstOrDefaultAsync(cancellationToken);

            return kanji ?? throw new NotFoundException(nameof(Kanji), request.Id);
        }
    }
}
