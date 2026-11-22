namespace JlptTrainer.Application.Common.ExcelImport;

public sealed record CatalogImportResult(
    int TotalRows,
    int AcceptedCount,
    int SkippedDuplicateCount,
    IReadOnlyList<CatalogImportRowError> Errors);

public sealed record CatalogImportRowError(int RowNumber, string Message);
