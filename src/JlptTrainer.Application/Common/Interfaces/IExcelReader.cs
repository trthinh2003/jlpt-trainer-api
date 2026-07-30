namespace JlptTrainer.Application.Common.Interfaces
{
    public interface IExcelReader
    {
        IReadOnlyList<IReadOnlyDictionary<string, string?>> ReadSheet(
            Stream fileStream,
            int sheetIndex = 0,
            int headerRowIndex = 1);
    }
}
