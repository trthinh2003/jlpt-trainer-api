using JlptTrainer.Application.Common.Interfaces;
using OfficeOpenXml;

namespace JlptTrainer.Infrastructure.ExcelImport
{
    public class EPPlusExcelReader : IExcelReader
    {
        public IReadOnlyList<IReadOnlyDictionary<string, string?>> ReadSheet(Stream fileStream, int sheetIndex = 0, int headerRowIndex = 1)
        {
            using var package = new ExcelPackage(fileStream);

            if (package.Workbook.Worksheets.Count == 0)
            {
                return [];
            }

            var worksheet = package.Workbook.Worksheets[sheetIndex];
            var dimension = worksheet.Dimension;

            if (dimension is null)
            {
                return []; // sheet trống hoàn toàn
            }

            var headers = new Dictionary<int, string>();
            for (var col = dimension.Start.Column; col <= dimension.End.Column; col++)
            {
                var headerText = worksheet.Cells[headerRowIndex, col].Text?.Trim();
                if (!string.IsNullOrEmpty(headerText))
                {
                    headers[col] = headerText;
                }
            }

            var rows = new List<IReadOnlyDictionary<string, string?>>();

            for (var row = headerRowIndex + 1; row <= dimension.End.Row; row++)
            {
                var rowData = new Dictionary<string, string?>();
                var isEmptyRow = true;

                foreach (var (col, headerName) in headers)
                {
                    var cellText = worksheet.Cells[row, col].Text;
                    rowData[headerName] = string.IsNullOrEmpty(cellText) ? null : cellText;

                    if (!string.IsNullOrWhiteSpace(cellText))
                    {
                        isEmptyRow = false;
                    }
                }
           
                if (!isEmptyRow)  // bỏ qua dòng trống hoàn toàn do user để lại cuối file
                {
                    rows.Add(rowData);
                }
            }

            return rows;
        }
    }
}
