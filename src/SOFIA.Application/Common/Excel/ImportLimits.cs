namespace SOFIA.Application.Common.Excel;

public static class ImportLimits
{
    // Upper bound for the uploaded .xlsx and for the JSON body of a bulk-load request
    public const long MaxRequestBytes = 5 * 1024 * 1024;

    public const int MaxRows = 5_000;

    public const long MaxUncompressedBytes = 50 * 1024 * 1024;

    public const int MaxCompressionRatio = 100;
}
