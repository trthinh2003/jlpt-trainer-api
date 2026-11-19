using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities;

public sealed class MockTestAttempt : BaseEntity
{
    private MockTestAttempt() { }

    public MockTestAttempt(Guid userId, JlptLevel level, DateTimeOffset startedAt, DateTimeOffset expiresAt)
    {
        UserId = userId;
        Level = level;
        CreatedAt = startedAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }
    public JlptLevel Level { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public List<MockTestAttemptQuestion> Questions { get; private set; } = [];
}
