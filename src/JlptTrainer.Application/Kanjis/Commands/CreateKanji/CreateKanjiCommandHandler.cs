using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Kanjis.Commands.CreateKanji
{
    public sealed class CreateKanjiCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateKanjiCommand, Guid>
    {
        public async Task<Guid> Handle(CreateKanjiCommand request, CancellationToken cancellationToken)
        {
            var character = request.Character.Trim();

            var exists = await dbContext.Kanjis.AnyAsync(k => k.Character == character, cancellationToken);
            if (exists)
            {
                throw new ConflictException($"Kanji \"{character}\" đã tồn tại trong hệ thống.");
            }

            var kanji = new Kanji
            {
                Character = character,
                OnYomi = request.OnYomi.Trim(),
                KunYomi = request.KunYomi.Trim(),
                Meaning = request.Meaning.Trim(),
                StrokeCount = request.StrokeCount,
                Level = request.Level
            };

            dbContext.Kanjis.Add(kanji);
            await dbContext.SaveChangesAsync(cancellationToken);

            return kanji.Id;
        }
    }
}
