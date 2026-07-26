using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
    {
        public void Configure(EntityTypeBuilder<StudySession> builder)
        {       
            builder.HasIndex(s => new { s.UserId, s.StartedAt }); // Dashboard/heatmap query theo UserId + khoảng ngày (CreatedAt) rất thường xuyên
        }
    }
}
