using System.IO.Compression;
using ClosedXML.Excel;
using SOFIA.Application.Common.Excel;
using SOFIA.Infrastructure.Excel;

namespace SOFIA.IntegrationTests.Excel;

public class ExcelReaderServiceTests
{
    private static readonly string[] Columns = ["NombreCompania", "CodigoIdentificador"];
    private readonly ExcelReaderService _reader = new();

    [Fact]
    public void ReadRows_ShouldReturnDataRows_WhenWorkbookIsValid()
    {
        using var stream = BuildWorkbook(3);

        var rows = _reader.ReadRows(stream, Columns, "plantilla.xlsx");

        _ = rows.Should().HaveCount(3);
        _ = rows[0].RowNumber.Should().Be(2);
        _ = rows[0].Values["nombreCompania"].Should().Be("Lab 1");
    }

    [Fact]
    public void ReadRows_ShouldAccept_WhenRowCountEqualsLimit()
    {
        using var stream = BuildWorkbook(ImportLimits.MaxRows);

        var rows = _reader.ReadRows(stream, Columns, "plantilla.xlsx");

        _ = rows.Should().HaveCount(ImportLimits.MaxRows);
    }

    [Fact]
    public void ReadRows_ShouldReject_WhenRowCountExceedsLimit()
    {
        using var stream = BuildWorkbook(ImportLimits.MaxRows + 1);

        var act = () => _reader.ReadRows(stream, Columns, "plantilla.xlsx");

        _ = act.Should().Throw<ExcelImportException>().Which.Error.Code.Should().Be(ExcelImportException.FilasExcedidas);
    }

    [Theory]
    [InlineData("datos.txt")]
    [InlineData("datos.xls")]
    [InlineData("datos.csv")]
    public void ReadRows_ShouldReject_WhenExtensionIsNotXlsx(string fileName)
    {
        using var stream = BuildWorkbook(1);

        var act = () => _reader.ReadRows(stream, Columns, fileName);

        _ = act.Should().Throw<ExcelImportException>().Which.Error.Code.Should().Be(ExcelImportException.ArchivoInvalido);
    }

    [Fact]
    public void ReadRows_ShouldReject_WhenContentIsNotAZipPackage()
    {
        using var stream = new MemoryStream("NombreCompania,CodigoIdentificador\nLab,1"u8.ToArray());

        var act = () => _reader.ReadRows(stream, Columns, "renombrado.xlsx");

        _ = act.Should().Throw<ExcelImportException>().Which.Error.Code.Should().Be(ExcelImportException.ArchivoInvalido);
    }

    [Fact]
    public void ReadRows_ShouldReject_WhenZipIsNotAWorkbook()
    {
        using var stream = BuildZip(("readme.txt", "hola"u8.ToArray(), CompressionLevel.Optimal));

        var act = () => _reader.ReadRows(stream, Columns, "otro.xlsx");

        _ = act.Should().Throw<ExcelImportException>().Which.Error.Code.Should().Be(ExcelImportException.ArchivoInvalido);
    }

    [Fact]
    public void ReadRows_ShouldReject_WhenAnEntryCompressionRatioExceedsLimit()
    {
        // 8 MB of zeros deflates to a few KB, far above the allowed ratio
        using var stream = BuildZip(("xl/media/pad.bin", new byte[8 * 1024 * 1024], CompressionLevel.Optimal));

        var act = () => _reader.ReadRows(stream, Columns, "bomba.xlsx");

        _ = act.Should().Throw<ExcelImportException>().Which.Error.Code.Should().Be(ExcelImportException.ArchivoExcedeDescompresion);
    }

    [Fact]
    public void ReadRows_ShouldReject_WhenTotalUncompressedSizeExceedsLimit()
    {
        // Stored entries keep the ratio at 1, so only the total size guard can reject this package
        var chunk = new byte[20 * 1024 * 1024];
        using var stream = BuildZip(
            ("a.bin", chunk, CompressionLevel.NoCompression),
            ("b.bin", chunk, CompressionLevel.NoCompression),
            ("c.bin", chunk, CompressionLevel.NoCompression));

        var act = () => _reader.ReadRows(stream, Columns, "grande.xlsx");

        _ = act.Should().Throw<ExcelImportException>().Which.Error.Code.Should().Be(ExcelImportException.ArchivoExcedeDescompresion);
    }

    private static MemoryStream BuildWorkbook(int dataRows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Datos");
        _ = sheet.Cell(1, 1).SetValue(Columns[0]);
        _ = sheet.Cell(1, 2).SetValue(Columns[1]);
        for (var i = 1; i <= dataRows; i++)
        {
            _ = sheet.Cell(i + 1, 1).SetValue($"Lab {i}");
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream BuildZip(params (string Name, byte[] Content, CompressionLevel Level)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content, level) in entries)
            {
                using var entryStream = archive.CreateEntry(name, level).Open();
                entryStream.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
