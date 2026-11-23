using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities;

public sealed class VideoLesson : IStoredMediaObject
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public JlptLevel Level { get; init; }
    public string ObjectKey { get; init; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CleanupAfter { get; set; }
}
