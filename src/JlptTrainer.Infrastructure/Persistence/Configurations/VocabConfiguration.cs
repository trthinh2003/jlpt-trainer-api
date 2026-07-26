using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class VocabConfiguration : IEntityTypeConfiguration<Vocab>
    {
        public void Configure(EntityTypeBuilder<Vocab> builder)
        {
            builder.Property(v => v.Word).HasMaxLength(100).IsRequired();
            builder.Property(v => v.Reading).HasMaxLength(100).IsRequired();
            builder.Property(v => v.Meaning).HasMaxLength(500).IsRequired();
    
            builder.HasIndex(v => v.Level);  // lọc theo Level rất thường xuyên (màn hình luyện tập lọc theo N5/N4...)
        }
    }
}
