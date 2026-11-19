namespace JlptTrainer.Application.Common.Interfaces
{
    public interface IExcelReader
    {
        IReadOnlyList<ExcelSheetRow> ReadSheet(
            Stream fileStream,
            int sheetIndex = 0,
            int headerRowIndex = 1);
    }

    public sealed record ExcelSheetRow(int Number, IReadOnlyDictionary<string, string?> Cells)
    {
        public string? Get(string column) => Cells.TryGetValue(column, out var value) ? value?.Trim() : null;
    }
}
