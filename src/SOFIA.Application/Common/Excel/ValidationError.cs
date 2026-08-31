namespace SOFIA.Application.Common.Excel;

public record ValidationError(
    string Code,
    string Field,
    Dictionary<string, object>? Params = null
);
