namespace JlptTrainer.Application.Vocabs.Images;

public interface IVocabImageStore
{
    Task StageAsync(string key, DateTimeOffset cleanupAfter, CancellationToken cancellationToken);
    Task SetCurrentAsync(Guid id, string? key, DateTimeOffset retireAfter, CancellationToken cancellationToken);
}
