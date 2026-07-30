namespace JlptTrainer.Application.Vocabs.ImportVocabFromExcel
{
    public sealed record ImportVocabResult(
        int TotalRows,
        int SuccessCount,
        int SkippedDuplicateCount,
        List<ImportRowError> Errors);
}
