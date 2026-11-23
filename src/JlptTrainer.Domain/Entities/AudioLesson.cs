using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities;

public sealed class AudioLesson : IStoredMediaObject
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public JlptLevel Level { get; init; }
    public string ObjectKey { get; init; } = "";
    public string CoverKey { get; init; } = "";
    public string Lyrics { get; init; } = "";
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CleanupAfter { get; set; }
    public List<AudioLessonWord> Words { get; } = [];
}

public sealed class AudioLessonWord
{
    public Guid AudioLessonId { get; init; }
    public Guid VocabId { get; init; }
    public Vocab Vocab { get; init; } = null!;
    public int Position { get; init; }
}
