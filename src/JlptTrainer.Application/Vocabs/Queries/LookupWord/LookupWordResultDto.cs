namespace JlptTrainer.Application.Vocabs.Queries.LookupWord
{
    public sealed record LookupWordResultDto(
        string Word,
        string Reading,
        List<string> Meanings,
        bool IsCommon);
}
