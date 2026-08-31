using ClosedXML.Excel;

namespace SOFIA.Infrastructure.Excel;

public static class ExcelTemplateGenerator
{
    public static byte[] GenerateTemplate(string[] columnNames)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Plantilla");

        for (var i = 0; i < columnNames.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = columnNames[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }

        _ = worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
