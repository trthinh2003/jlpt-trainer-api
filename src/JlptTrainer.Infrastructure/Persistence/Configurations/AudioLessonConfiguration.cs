using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations;

public sealed class AudioLessonConfiguration : IEntityTypeConfiguration<AudioLesson>
{
    public void Configure(EntityTypeBuilder<AudioLesson> builder)
    {
        builder.Property(lesson => lesson.Title).HasMaxLength(150);
        builder.Property(lesson => lesson.ObjectKey).HasMaxLength(160);
        builder.Property(lesson => lesson.CoverKey).HasMaxLength(160);
        builder.Property(lesson => lesson.Lyrics).HasMaxLength(28000);
        builder.HasIndex(lesson => new { lesson.PublishedAt, lesson.Id }).HasFilter("published_at IS NOT NULL");
        builder.HasIndex(lesson => lesson.CleanupAfter).HasFilter("published_at IS NULL");
        builder.HasMany(lesson => lesson.Words).WithOne().HasForeignKey(word => word.AudioLessonId);
    }
}

public sealed class AudioLessonWordConfiguration : IEntityTypeConfiguration<AudioLessonWord>
{
    public void Configure(EntityTypeBuilder<AudioLessonWord> builder)
    {
        builder.ToTable("audio_lesson_words");
        builder.HasKey(word => new { word.AudioLessonId, word.VocabId });
        builder.HasOne(word => word.Vocab).WithMany().HasForeignKey(word => word.VocabId);
    }
}
