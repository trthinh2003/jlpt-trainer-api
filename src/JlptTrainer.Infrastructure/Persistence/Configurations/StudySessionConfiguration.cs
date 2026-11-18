using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
    {
        public void Configure(EntityTypeBuilder<StudySession> builder)
        {
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("ck_study_sessions_valid_review_counts", "correct_count >= 0 AND correct_count <= cards_reviewed");
                table.HasCheckConstraint("ck_study_sessions_valid_end_time", "ended_at IS NULL OR ended_at >= started_at");
            });

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.UserId, s.StartedAt });
        }
    }
}
