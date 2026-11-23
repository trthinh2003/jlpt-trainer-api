using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations;

public sealed class VideoLessonConfiguration : IEntityTypeConfiguration<VideoLesson>
{
    public void Configure(EntityTypeBuilder<VideoLesson> builder)
    {
        builder.Property(lesson => lesson.Title).HasMaxLength(150);
        builder.Property(lesson => lesson.Description).HasMaxLength(1000);
        builder.Property(lesson => lesson.ObjectKey).HasMaxLength(160);
        builder.HasIndex(lesson => new { lesson.PublishedAt, lesson.Id }).HasFilter("published_at IS NOT NULL");
        builder.HasIndex(lesson => lesson.CleanupAfter).HasFilter("published_at IS NULL");
    }
}
