using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
            builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();

            builder.HasIndex(u => u.Email).IsUnique();
        }
    }
}
