namespace JlptTrainer.Application.Common.Interfaces
{
    public interface IExcelTemplateGenerator
    {
        byte[] Generate(string sheetName, string[] headers, IEnumerable<string[]>? sampleRows = null);
    }
}
