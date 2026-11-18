using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class ReviewCardConfiguration : IEntityTypeConfiguration<ReviewCard>
    {
        public void Configure(EntityTypeBuilder<ReviewCard> builder)
        {
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("ck_review_cards_nonnegative_interval", "interval_days >= 0");
                table.HasCheckConstraint("ck_review_cards_nonnegative_repetitions", "repetitions >= 0");
            });

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.UserId, c.NextReviewDate });

            builder.HasIndex(c => new { c.UserId, c.CardType, c.ReferenceId })
                .IsUnique();  // 1 user không thể có 2 ReviewCard trỏ cùng 1 nội dung (Vocab/Kanji/Grammar) trùng CardType
        }
    }
}
