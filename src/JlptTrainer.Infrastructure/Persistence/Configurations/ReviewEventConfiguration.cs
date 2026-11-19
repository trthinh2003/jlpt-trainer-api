using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations;

public sealed class ReviewEventConfiguration : IEntityTypeConfiguration<ReviewEvent>
{
    public void Configure(EntityTypeBuilder<ReviewEvent> builder)
    {
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        // Mã thẻ là thông tin lịch sử và được giữ lại khi thẻ bị xóa.
        builder.HasIndex(e => new { e.UserId, e.ReviewedAt });
        builder.HasIndex(e => new { e.UserId, e.RequestId }).HasFilter("request_id IS NOT NULL").IsUnique();
    }
}
