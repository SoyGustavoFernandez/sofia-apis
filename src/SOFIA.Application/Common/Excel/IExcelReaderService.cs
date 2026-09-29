namespace SOFIA.Application.Common.Excel;

public interface IExcelReaderService
{
    // Throws ExcelImportException when the file is not a safe .xlsx or exceeds ImportLimits
    List<ExcelRow> ReadRows(Stream stream, IReadOnlyList<string> expectedColumns, string? fileName = null);
}
