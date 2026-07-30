using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.UnitTests
{
    public sealed class ApplicationDbContextFake : DbContext, IApplicationDbContext
    {
        public ApplicationDbContextFake()
            : base(new DbContextOptionsBuilder()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Vocab> Vocabs => Set<Vocab>();
        public DbSet<Kanji> Kanjis => Set<Kanji>();
        public DbSet<GrammarPoint> GrammarPoints => Set<GrammarPoint>();
        public DbSet<ReviewCard> ReviewCards => Set<ReviewCard>();
        public DbSet<StudySession> StudySessions => Set<StudySession>();
        public DbSet<MockTest> MockTests => Set<MockTest>();
    }
}
