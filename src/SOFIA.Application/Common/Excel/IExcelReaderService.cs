namespace SOFIA.Application.Common.Excel;

public interface IExcelReaderService
{
    List<ExcelRow> ReadRows(Stream stream, IReadOnlyList<string> expectedColumns);
}
