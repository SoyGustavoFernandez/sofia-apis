namespace SOFIA.Application.Common.Excel;

public record PreviewRowResult
{
    public int RowNumber { get; init; }
    public Dictionary<string, string?> Data { get; init; } = [];
    public List<ValidationError> Errors { get; init; } = [];
    public bool IsValid => Errors.Count == 0;
}
