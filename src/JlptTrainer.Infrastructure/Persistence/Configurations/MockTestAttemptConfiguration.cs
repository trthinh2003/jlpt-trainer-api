using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JlptTrainer.Infrastructure.Persistence.Configurations;

public sealed class MockTestAttemptConfiguration : IEntityTypeConfiguration<MockTestAttempt>
{
    public void Configure(EntityTypeBuilder<MockTestAttempt> builder)
    {
        builder.HasOne<User>().WithMany().HasForeignKey(attempt => attempt.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(attempt => attempt.Questions).WithOne().HasForeignKey(question => question.AttemptId);
        builder.ToTable(table => table.HasCheckConstraint("ck_mock_test_attempts_expiry", "expires_at > created_at"));
    }
}
