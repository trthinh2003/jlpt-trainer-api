using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations;

public sealed class VocabImageConfiguration : IEntityTypeConfiguration<VocabImage>
{
    public void Configure(EntityTypeBuilder<VocabImage> builder)
    {
        builder.ToTable("vocab_images");
        builder.HasKey(image => image.ObjectKey);
        builder.Property(image => image.ObjectKey).HasMaxLength(160);
        builder.HasIndex(image => image.CleanupAfter).HasFilter("vocab_id IS NULL");
    }
}
