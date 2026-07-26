using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Vocab> Vocabs { get; }
        DbSet<Kanji> Kanjis { get; }
        DbSet<GrammarPoint> GrammarPoints { get; }
        DbSet<ReviewCard> ReviewCards { get; }
        DbSet<StudySession> StudySessions { get; }
        DbSet<MockTest> MockTests { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
