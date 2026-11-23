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
        public DbSet<VideoLesson> VideoLessons => Set<VideoLesson>();
        public DbSet<AudioLesson> AudioLessons => Set<AudioLesson>();
        public DbSet<Kanji> Kanjis => Set<Kanji>();
        public DbSet<GrammarPoint> GrammarPoints => Set<GrammarPoint>();
        public DbSet<ReviewCard> ReviewCards => Set<ReviewCard>();
        public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
        public DbSet<StudySession> StudySessions => Set<StudySession>();
        public DbSet<MockTest> MockTests => Set<MockTest>();
        public DbSet<MockTestAttempt> MockTestAttempts => Set<MockTestAttempt>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VocabImage>().HasKey(image => image.ObjectKey);
            modelBuilder.Entity<AudioLessonWord>().HasKey(word => new { word.AudioLessonId, word.VocabId });
            modelBuilder.Entity<Vocab>().HasOne(vocab => vocab.Image).WithOne()
                .HasForeignKey<VocabImage>(image => image.VocabId);
            modelBuilder.Entity<MockTestAttemptQuestion>().HasKey(question => new { question.AttemptId, question.Position });
            base.OnModelCreating(modelBuilder);
        }
    }
}
