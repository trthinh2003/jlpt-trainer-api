using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class ReviewCardConfiguration : IEntityTypeConfiguration<ReviewCard>
    {
        public void Configure(EntityTypeBuilder<ReviewCard> builder)
        {
            builder.Property(c => c.EaseFactor)
                .HasPrecision(4, 2);

            builder.HasIndex(c => new { c.UserId, c.NextReviewDate });

           
            builder.HasIndex(c => new { c.UserId, c.CardType, c.ReferenceId })
                .IsUnique();  // 1 user không thể có 2 ReviewCard trỏ cùng 1 nội dung (Vocab/Kanji/Grammar) trùng CardType
        }
    }
}
