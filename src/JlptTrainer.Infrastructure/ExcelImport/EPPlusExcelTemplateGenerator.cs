using JlptTrainer.Application.Common.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace JlptTrainer.Infrastructure.ExcelImport
{
    public class EPPlusExcelTemplateGenerator : IExcelTemplateGenerator
    {
        public byte[] Generate(string sheetName, string[] headers, IEnumerable<string[]>? sampleRows = null)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            for (var col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cells[1, col + 1];
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            if (sampleRows is not null)
            {
                var rowIndex = 2;
                foreach (var row in sampleRows)
                {
                    for (var col = 0; col < row.Length; col++)
                    {
                        worksheet.Cells[rowIndex, col + 1].Value = row[col];
                    }
                    rowIndex++;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}
