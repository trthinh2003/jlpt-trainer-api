using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class KanjiConfiguration : IEntityTypeConfiguration<Kanji>
    {
        public void Configure(EntityTypeBuilder<Kanji> builder)
        {
            builder.Property(k => k.Character).HasMaxLength(10).IsRequired();
            builder.Property(k => k.OnYomi).HasMaxLength(200);
            builder.Property(k => k.KunYomi).HasMaxLength(200);
            builder.Property(k => k.Meaning).HasMaxLength(500).IsRequired();

            builder.HasIndex(k => k.Level);

            // 1 ký tự kanji chỉ nên xuất hiện 1 lần trong bảng, tránh trùng lặp khi import Excel
            builder.HasIndex(k => k.Character).IsUnique();
        }
    }
}
