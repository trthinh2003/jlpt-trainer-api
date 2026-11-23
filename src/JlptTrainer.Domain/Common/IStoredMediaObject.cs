namespace JlptTrainer.Domain.Common;

public interface IStoredMediaObject
{
    string ObjectKey { get; }
    DateTimeOffset CleanupAfter { get; set; }
}
