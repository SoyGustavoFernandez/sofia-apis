using System.IO.Compression;
using ClosedXML.Excel;
using SOFIA.Application.Common.Excel;
using SOFIA.Domain.Common;

namespace SOFIA.Infrastructure.Excel;

public class ExcelReaderService : IExcelReaderService
{
    // Small parts (styles, content types) compress extremely well, so the ratio guard only applies above this size
    private const long RatioCheckMinBytes = 1024 * 1024;

    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public List<ExcelRow> ReadRows(Stream stream, IReadOnlyList<string> expectedColumns, string? fileName = null)
    {
        if (fileName is not null && !string.Equals(Path.GetExtension(fileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidFile();
        }

        using var copy = stream.CanSeek ? null : new MemoryStream();
        var source = copy ?? stream;
        if (copy is not null)
        {
            stream.CopyTo(copy);
        }

        EnsureZipSignature(source);
        EnsureSafeDecompression(source);

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(source);
        }
        catch (Exception ex) when (ex is not ExcelImportException and not OperationCanceledException)
        {
            throw InvalidFile();
        }

        using (workbook)
        {
            return ReadWorksheet(workbook.Worksheets.First(), expectedColumns);
        }
    }

    private static List<ExcelRow> ReadWorksheet(IXLWorksheet worksheet, IReadOnlyList<string> expectedColumns)
    {
        var rows = new List<ExcelRow>();
        var headerMap = BuildHeaderMap(worksheet);

        // RowsUsed skips blank rows without materializing them, unlike looping up to LastRowUsed
        foreach (var row in worksheet.RowsUsed().Where(r => r.RowNumber() > 1))
        {
            var values = ReadValues(row, headerMap, expectedColumns);
            if (values is null)
            {
                continue;
            }

            if (rows.Count == ImportLimits.MaxRows)
            {
                throw new ExcelImportException(Error.Validation(
                    ExcelImportException.FilasExcedidas,
                    $"The file exceeds the maximum of {ImportLimits.MaxRows} rows."));
            }

            rows.Add(new ExcelRow(row.RowNumber(), values));
        }

        return rows;
    }

    // Build header map from first row (case-insensitive)
    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
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

        return headerMap;
    }

    // Returns null for completely empty rows so they are skipped
    private static Dictionary<string, string?>? ReadValues(IXLRow row, Dictionary<string, int> headerMap, IReadOnlyList<string> expectedColumns)
    {
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

            values[char.ToLowerInvariant(col[0]) + col[1..]] = cellValue;
            hasAnyValue |= cellValue != null;
        }

        return hasAnyValue ? values : null;
    }

    private static void EnsureZipSignature(Stream stream)
    {
        Span<byte> header = stackalloc byte[4];
        stream.Position = 0;
        var read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        stream.Position = 0;

        if (read < header.Length || !header.SequenceEqual(ZipSignature))
        {
            throw InvalidFile();
        }
    }

    // Inflates every part once while counting bytes, since the sizes declared in the zip directory can lie
    private static void EnsureSafeDecompression(Stream stream)
    {
        var buffer = new byte[81920];
        long total = 0;

        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            foreach (var entry in archive.Entries)
            {
                using var entryStream = entry.Open();
                long entryBytes = 0;
                int read;
                while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    entryBytes += read;
                    total += read;

                    var ratioExceeded = entryBytes > RatioCheckMinBytes
                        && entryBytes > Math.Max(entry.CompressedLength, 1) * ImportLimits.MaxCompressionRatio;
                    if (total > ImportLimits.MaxUncompressedBytes || ratioExceeded)
                    {
                        throw new ExcelImportException(Error.Validation(
                            ExcelImportException.ArchivoExcedeDescompresion,
                            "The file expands beyond the allowed decompression limits."));
                    }
                }
            }
        }
        catch (InvalidDataException)
        {
            throw InvalidFile();
        }
        finally
        {
            stream.Position = 0;
        }
    }

    private static ExcelImportException InvalidFile() =>
        new(Error.Validation(ExcelImportException.ArchivoInvalido, "The file is not a valid .xlsx workbook."));
}
