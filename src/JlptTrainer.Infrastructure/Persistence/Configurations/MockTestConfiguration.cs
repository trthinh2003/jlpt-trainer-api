using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations
{
    public class MockTestConfiguration : IEntityTypeConfiguration<MockTest>
    {
        public void Configure(EntityTypeBuilder<MockTest> builder)
        {
            builder.ToTable(table => table.HasCheckConstraint(
                "ck_mock_tests_valid_answer_counts", "correct_answers >= 0 AND correct_answers <= total_questions"));

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Ignore(m => m.ScorePercentage); // - không map vào cột DB, tránh EF tạo cột "score_percentage" thừa.

            builder.HasIndex(m => new { m.UserId, m.TakenAt });
            builder.HasOne<MockTestAttempt>()
                .WithOne()
                .HasForeignKey<MockTest>(m => m.AttemptId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.ToTable(table => table.HasCheckConstraint(
                "ck_mock_tests_attempt_answers",
                "(attempt_id IS NULL AND selected_choices IS NULL) OR " +
                "(attempt_id IS NOT NULL AND selected_choices IS NOT NULL AND cardinality(selected_choices) = total_questions)"));
        }
    }
}
