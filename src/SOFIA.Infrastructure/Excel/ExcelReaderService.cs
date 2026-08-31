using ClosedXML.Excel;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Infrastructure.Excel;

public class ExcelReaderService : IExcelReaderService
{
    public List<ExcelRow> ReadRows(Stream stream, IReadOnlyList<string> expectedColumns)
    {
        var rows = new List<ExcelRow>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        // Build header map from first row (case-insensitive)
        var headerRow = worksheet.Row(1);
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var lastUsedCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var col = 1; col <= lastUsedCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(header) && !headerMap.ContainsKey(header))
            {
                headerMap[header] = col;
            }
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);

            // Skip completely empty rows
            var hasAnyValue = false;
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var col in expectedColumns)
            {
                string? cellValue = null;
                if (headerMap.TryGetValue(col, out var colIndex))
                {
                    var raw = row.Cell(colIndex).GetString().Trim();
                    cellValue = string.IsNullOrEmpty(raw) ? null : raw;
                }

                values[col.ToLowerInvariant()] = cellValue;
                if (cellValue != null)
                {
                    hasAnyValue = true;
                }
            }

            if (!hasAnyValue)
            {
                continue;
            }

            rows.Add(new ExcelRow(rowNumber, values));
        }

        return rows;
    }
}
