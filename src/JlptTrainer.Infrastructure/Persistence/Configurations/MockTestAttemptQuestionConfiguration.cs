using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations;

public sealed class MockTestAttemptQuestionConfiguration : IEntityTypeConfiguration<MockTestAttemptQuestion>
{
    public void Configure(EntityTypeBuilder<MockTestAttemptQuestion> builder)
    {
        builder.ToTable("mock_test_attempt_questions", table => table.HasCheckConstraint(
            "ck_mock_test_attempt_questions_choices",
            "cardinality(choices) = 4 AND correct_choice_index >= 0 AND correct_choice_index < cardinality(choices)"));
        builder.HasKey(question => new { question.AttemptId, question.Position });
        builder.Property(question => question.Word).HasMaxLength(100);
        builder.Property(question => question.Reading).HasMaxLength(100);
    }
}
