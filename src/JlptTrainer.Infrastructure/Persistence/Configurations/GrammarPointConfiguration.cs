using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class GrammarPointConfiguration : IEntityTypeConfiguration<GrammarPoint>
    {
        public void Configure(EntityTypeBuilder<GrammarPoint> builder)
        {
            builder.Property(g => g.Pattern).HasMaxLength(200).IsRequired();
            builder.Property(g => g.Meaning).HasMaxLength(500).IsRequired();

            builder.HasIndex(g => g.Level);
        }
    }
}
