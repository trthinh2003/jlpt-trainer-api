namespace JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel
{
    public sealed record ImportGrammarPointResult(
        int TotalRows,
        int SuccessCount,
        int SkippedDuplicateCount,
        List<ImportGrammarPointRowError> Errors);
}
