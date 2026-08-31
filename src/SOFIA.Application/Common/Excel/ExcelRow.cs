namespace SOFIA.Application.Common.Excel;

public record ExcelRow(int RowNumber, Dictionary<string, string?> Values);
