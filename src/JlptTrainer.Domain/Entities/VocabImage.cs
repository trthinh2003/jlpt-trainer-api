using JlptTrainer.Domain.Common;

namespace JlptTrainer.Domain.Entities;

public sealed class VocabImage : IStoredMediaObject
{
    public string ObjectKey { get; init; } = string.Empty;
    public Guid? VocabId { get; set; }
    public DateTimeOffset CleanupAfter { get; set; }
}
