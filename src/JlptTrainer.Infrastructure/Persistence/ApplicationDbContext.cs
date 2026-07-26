using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace JlptTrainer.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Vocab> Vocabs => Set<Vocab>();
        public DbSet<Kanji> Kanjis => Set<Kanji>();
        public DbSet<GrammarPoint> GrammarPoints => Set<GrammarPoint>();
        public DbSet<ReviewCard> ReviewCards => Set<ReviewCard>();
        public DbSet<StudySession> StudySessions => Set<StudySession>();
        public DbSet<MockTest> MockTests => Set<MockTest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
    }
}
