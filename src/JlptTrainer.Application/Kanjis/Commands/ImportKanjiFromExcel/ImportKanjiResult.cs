namespace JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel
{
    public sealed record ImportKanjiResult(
       int TotalRows,
       int SuccessCount,
       int SkippedDuplicateCount,
       List<ImportKanjiRowError> Errors);
}
