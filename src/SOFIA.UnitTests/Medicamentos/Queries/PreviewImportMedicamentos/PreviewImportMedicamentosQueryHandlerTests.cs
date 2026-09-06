using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Medicamentos.Queries.PreviewImportMedicamentos;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Queries.PreviewImportMedicamentos;

public class PreviewImportMedicamentosQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly PreviewImportMedicamentosQueryHandler _handler;

    public PreviewImportMedicamentosQueryHandlerTests()
    {
        var existente = Medicamento.Create("EXISTE-01", "Ya Existe", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { existente }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(new List<Laboratorio> { Laboratorio.Create("Bayer S.A.", null).Value! }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(new List<UnidadMedida> { UnidadMedida.Create("TAB", "Tableta").Value! }.BuildMockDbSet().Object);
        _handler = new PreviewImportMedicamentosQueryHandler(_dbContextMock.Object);
    }

    private static ExcelRow Row(
        int number = 1,
        string? codigo = "COD-001",
        string? nombre = "Paracetamol",
        string? lab = "Bayer S.A.",
        string? unidad = "Tableta",
        string? condicion = "Venta Libre (OTC)") =>
        new(number, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["codigoNacional"] = codigo,
            ["nombreComercial"] = nombre,
            ["laboratorio"] = lab,
            ["unidadBase"] = unidad,
            ["condicionVenta"] = condicion,
        });

    private async Task<PreviewRowResult> Preview(ExcelRow row)
    {
        var result = await _handler.Handle(new PreviewImportMedicamentosQuery([row]), CancellationToken.None);
        return result.Rows.Single();
    }

    [Fact]
    public async Task Handle_ShouldReturnNoErrors_WhenRowValid()
    {
        var row = await Preview(Row());

        _ = row.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "codigoNacional")]
    [InlineData("", "codigoNacional")]
    public async Task Handle_ShouldFlagRequired_WhenCodigoMissing(string? codigo, string field)
    {
        var row = await Preview(Row(codigo: codigo));

        _ = row.Errors.Should().Contain(e => e.Code == "required" && e.Field == field);
    }

    [Fact]
    public async Task Handle_ShouldFlagRequired_WhenNombreMissing()
    {
        var row = await Preview(Row(nombre: " "));

        _ = row.Errors.Should().Contain(e => e.Code == "required" && e.Field == "nombreComercial");
    }

    [Fact]
    public async Task Handle_ShouldFlagMaxLength_WhenCodigoTooLong()
    {
        var row = await Preview(Row(codigo: new string('x', Medicamento.CodigoNacionalMaxLength + 1)));

        _ = row.Errors.Should().Contain(e => e.Code == "max-length" && e.Field == "codigoNacional");
    }

    [Fact]
    public async Task Handle_ShouldFlagMaxLength_WhenNombreTooLong()
    {
        var row = await Preview(Row(nombre: new string('x', Medicamento.NombreComercialMaxLength + 1)));

        _ = row.Errors.Should().Contain(e => e.Code == "max-length" && e.Field == "nombreComercial");
    }

    [Fact]
    public async Task Handle_ShouldFlagDuplicate_WhenCodigoExistsInDb()
    {
        var row = await Preview(Row(codigo: "EXISTE-01"));

        _ = row.Errors.Should().Contain(e => e.Code == "duplicate" && e.Field == "codigoNacional");
    }

    [Fact]
    public async Task Handle_ShouldFlagDuplicateInFile_WhenCodigoRepeated()
    {
        var result = await _handler.Handle(
            new PreviewImportMedicamentosQuery([Row(1, codigo: "DUP-01"), Row(2, codigo: "DUP-01")]),
            CancellationToken.None);

        _ = result.Rows.Should().OnlyContain(r => r.Errors.Any(e => e.Code == "duplicate-in-file" && e.Field == "codigoNacional"));
    }

    [Fact]
    public async Task Handle_ShouldFlagNotFound_WhenLaboratorioNotInCatalog()
    {
        var row = await Preview(Row(lab: "Inexistente"));

        _ = row.Errors.Should().Contain(e => e.Code == "not-found" && e.Field == "laboratorio");
    }

    [Fact]
    public async Task Handle_ShouldFlagNotFound_WhenUnidadNotInCatalog()
    {
        var row = await Preview(Row(unidad: "Inexistente"));

        _ = row.Errors.Should().Contain(e => e.Code == "not-found" && e.Field == "unidadBase");
    }

    [Fact]
    public async Task Handle_ShouldFlagInvalidOption_WhenCondicionVentaUnknown()
    {
        var row = await Preview(Row(condicion: "No existe"));

        _ = row.Errors.Should().Contain(e => e.Code == "invalid-option" && e.Field == "condicionVenta");
    }

    [Fact]
    public async Task Handle_ShouldComputeCounts()
    {
        var result = await _handler.Handle(
            new PreviewImportMedicamentosQuery([Row(1), Row(2, codigo: null)]),
            CancellationToken.None);

        _ = result.TotalCount.Should().Be(2);
        _ = result.ValidCount.Should().Be(1);
        _ = result.ErrorCount.Should().Be(1);
    }
}
