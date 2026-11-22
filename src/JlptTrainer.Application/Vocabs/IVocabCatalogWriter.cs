using JlptTrainer.Domain.Entities;

namespace JlptTrainer.Application.Vocabs;

public interface IVocabCatalogWriter
{
    Task<int> InsertMissingAsync(IReadOnlyList<Vocab> vocabs, CancellationToken cancellationToken);
}
