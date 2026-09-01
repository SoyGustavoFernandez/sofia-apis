using ClosedXML.Excel;

namespace SOFIA.Infrastructure.Excel;

public static class ExcelTemplateGenerator
{
    private static readonly XLColor HeaderBackground = XLColor.FromHtml("#1E88E5");
    private static readonly XLColor HeaderForeground = XLColor.White;
    private static readonly XLColor AltRowBackground = XLColor.FromHtml("#F5F9FF");

    public static byte[] GenerateTemplate(string[] columnNames)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Plantilla");
        ApplyHeader(worksheet, columnNames);
        _ = worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] GenerateReport(string[] headers, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Data");

        ApplyHeader(ws, headers);

        var rowIndex = 2;
        foreach (var dataRow in rows)
        {
            for (var col = 0; col < dataRow.Length; col++)
            {
                var cell = ws.Cell(rowIndex, col + 1);
                cell.Value = dataRow[col] switch
                {
                    null => XLCellValue.FromObject(string.Empty),
                    string s => XLCellValue.FromObject(s),
                    int n => XLCellValue.FromObject(n),
                    long l => XLCellValue.FromObject(l),
                    double d => XLCellValue.FromObject(d),
                    decimal m => XLCellValue.FromObject((double)m),
                    bool b => XLCellValue.FromObject(b),
                    DateTime dt => XLCellValue.FromObject(dt),
                    DateTimeOffset dto => XLCellValue.FromObject(dto.DateTime),
                    _ => XLCellValue.FromObject(dataRow[col]!.ToString()!)
                };

                if (rowIndex % 2 == 0)
                {
                    cell.Style.Fill.BackgroundColor = AltRowBackground;
                }
            }
            rowIndex++;
        }

        _ = ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ApplyHeader(IXLWorksheet ws, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = HeaderForeground;
            cell.Style.Fill.BackgroundColor = HeaderBackground;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }
}
