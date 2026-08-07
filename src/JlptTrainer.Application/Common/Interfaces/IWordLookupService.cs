namespace JlptTrainer.Application.Common.Interfaces
{
    public interface IWordLookupService
    {
        Task<List<WordLookupResult>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    }

    public sealed record WordLookupResult(
        string Word,
        string Reading,
        List<string> Meanings,
        bool IsCommon);
}
