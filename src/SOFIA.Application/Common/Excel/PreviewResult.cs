namespace SOFIA.Application.Common.Excel;

public record PreviewResult
{
    public List<PreviewRowResult> Rows { get; init; } = [];
    public int TotalCount => Rows.Count;
    public int ValidCount => Rows.Count(r => r.IsValid);
    public int ErrorCount => Rows.Count(r => !r.IsValid);
}
