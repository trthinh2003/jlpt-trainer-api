using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class MockTestConfiguration : IEntityTypeConfiguration<MockTest>
    {
        public void Configure(EntityTypeBuilder<MockTest> builder)
        {            
            builder.Ignore(m => m.ScorePercentage); // - không map vào cột DB, tránh EF tạo cột "score_percentage" thừa.

            builder.HasIndex(m => new { m.UserId, m.TakenAt });
        }
    }
}
