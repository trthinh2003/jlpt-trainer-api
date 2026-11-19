using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Common.Exceptions;
using OfficeOpenXml;
using System.Xml;

namespace JlptTrainer.Infrastructure.ExcelImport
{
    public class EPPlusExcelReader : IExcelReader
    {
        public IReadOnlyList<ExcelSheetRow> ReadSheet(Stream fileStream, int sheetIndex = 0, int headerRowIndex = 1)
        {
            using var package = OpenWorkbook(fileStream);

            if (sheetIndex >= package.Workbook.Worksheets.Count)
            {
                return [];
            }

            var worksheet = package.Workbook.Worksheets[sheetIndex];
            var dimension = worksheet.DimensionByValue;

            if (dimension is null)
            {
                return []; // sheet trống hoàn toàn
            }

            var headers = new Dictionary<int, string>();
            var headerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var col = dimension.Start.Column; col <= dimension.End.Column; col++)
            {
                var headerText = worksheet.Cells[headerRowIndex, col].Text?.Trim();
                if (!string.IsNullOrEmpty(headerText))
                {
                    if (!headerNames.Add(headerText))
                    {
                        throw new ExcelWorkbookException($"Tên cột \"{headerText}\" xuất hiện nhiều lần.");
                    }
                    headers[col] = headerText;
                }
            }

            if (headers.Count == 0)
            {
                throw new ExcelWorkbookException("Thiếu tiêu đề cột ở dòng đầu của trang tính.");
            }

            var rows = new List<ExcelSheetRow>();

            for (var row = headerRowIndex + 1; row <= dimension.End.Row; row++)
            {
                var rowData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
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
                    rows.Add(new ExcelSheetRow(row, rowData));
                }
            }

            return rows;
        }

        private static ExcelPackage OpenWorkbook(Stream stream)
        {
            try
            {
                return new ExcelPackage(stream);
            }
            catch (Exception error) when (error is InvalidDataException or IOException or XmlException)
            {
                throw new ExcelWorkbookException("Không đọc được file Excel. Hãy chọn file .xlsx hợp lệ.", error);
            }
        }
    }
}
