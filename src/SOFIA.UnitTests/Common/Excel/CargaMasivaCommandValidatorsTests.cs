using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.DIGEMID.Commands.CargaMasivaDigemid;
using SOFIA.Application.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;
using SOFIA.Application.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;
using SOFIA.Application.Laboratorios.Commands.CargaMasivaLaboratorios;
using SOFIA.Application.Medicamentos.Commands.CargaMasivaMedicamentos;
using SOFIA.Application.Pacientes.Commands.CargaMasivaPacientes;
using SOFIA.Application.Profesionales.Commands.CargaMasivaProfesionalesSalud;
using SOFIA.Application.Proveedores.Commands.CargaMasivaProveedores;
using SOFIA.Application.Security.Commands.Roles.CargaMasivaRoles;
using SOFIA.Application.Sucursales.Commands.CargaMasivaSucursales;
using SOFIA.Application.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Common.Excel;

public class CargaMasivaCommandValidatorsTests
{
    private sealed record Case(Func<int, ValidationResult> WithValidRows, Func<ValidationResult> WithTooLongField, string TooLongProperty);

    private static readonly Dictionary<string, Case> Cases = new()
    {
        ["Digemid"] = For(
            new CargaMasivaDigemidCommandValidator(), rows => new CargaMasivaDigemidCommand(rows),
            new DigemidImportRow("P001", "Paracetamol", null, null, null, null, null, "Activo"),
            new DigemidImportRow(Long(21), "Paracetamol", null, null, null, null, null, "Activo"), "CodProd"),
        ["IngredientesActivos"] = For(
            new CargaMasivaIngredientesActivosCommandValidator(), rows => new CargaMasivaIngredientesActivosCommand(rows),
            new IngredienteActivoImportRow("Paracetamol", "N02BE01"),
            new IngredienteActivoImportRow("Paracetamol", Long(16)), "CodigoAtc"),
        ["JerarquiasUoM"] = For(
            new CargaMasivaJerarquiasUoMCommandValidator(), rows => new CargaMasivaJerarquiasUoMCommand(rows),
            new JerarquiaUoMImportRow("Paracetamol", "Caja", "Tableta", "10"),
            new JerarquiaUoMImportRow("Paracetamol", Long(51), "Tableta", "10"), "UnidadMayor"),
        ["Laboratorios"] = For(
            new CargaMasivaLaboratoriosCommandValidator(), rows => new CargaMasivaLaboratoriosCommand(rows),
            new LaboratorioImportRow("Bayer", null),
            new LaboratorioImportRow(Long(151), null), "NombreCompania"),
        ["Medicamentos"] = For(
            new CargaMasivaMedicamentosCommandValidator(), rows => new CargaMasivaMedicamentosCommand(rows),
            new MedicamentoImportRow("COD-1", "Paracetamol", "Bayer", "Tableta", "Venta Libre (OTC)"),
            new MedicamentoImportRow(Long(Medicamento.CodigoNacionalMaxLength + 1), "Paracetamol", "Bayer", "Tableta", "Venta Libre (OTC)"), "CodigoNacional"),
        ["Pacientes"] = For(
            new CargaMasivaPacientesCommandValidator(), rows => new CargaMasivaPacientesCommand(rows),
            new PacienteImportRow("12345678", "Juan Perez", "01/01/1990", null),
            new PacienteImportRow("12345678", Long(201), "01/01/1990", null), "NombreApellidos"),
        ["ProfesionalesSalud"] = For(
            new CargaMasivaProfesionalesSaludCommandValidator(), rows => new CargaMasivaProfesionalesSaludCommand(rows),
            new ProfesionalSaludImportRow("CMP-1", "Dra. Rojas", null),
            new ProfesionalSaludImportRow("CMP-1", "Dra. Rojas", Long(256)), "DireccionClinica"),
        ["Proveedores"] = For(
            new CargaMasivaProveedoresCommandValidator(), rows => new CargaMasivaProveedoresCommand(rows),
            new ProveedorImportRow("Distribuidora", "20123456789", null, null, 90m),
            new ProveedorImportRow("Distribuidora", Long(51), null, null, 90m), "TaxId"),
        ["Roles"] = For(
            new CargaMasivaRolesCommandValidator(), rows => new CargaMasivaRolesCommand(rows),
            new RolImportRow("Cajero", null, 1),
            new RolImportRow("Cajero", Long(256), 1), "Descripcion"),
        ["Sucursales"] = For(
            new CargaMasivaSucursalesCommandValidator(), rows => new CargaMasivaSucursalesCommand(rows),
            new SucursalImportRow("Central", "Av. Arequipa 123", "LIC-1"),
            new SucursalImportRow(Long(101), "Av. Arequipa 123", "LIC-1"), "Nombre"),
        ["UnidadesMedida"] = For(
            new CargaMasivaUnidadesMedidaCommandValidator(), rows => new CargaMasivaUnidadesMedidaCommand(rows),
            new UnidadMedidaImportRow("TAB", "Tableta"),
            new UnidadMedidaImportRow(Long(11), "Tableta"), "Codigo"),
    };

    public static TheoryData<string> CaseNames => [.. Cases.Keys];

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Validate_ShouldPass_WhenRowsAreValid(string name)
    {
        var result = Cases[name].WithValidRows(1);

        _ = result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Validate_ShouldPass_WhenRowCountEqualsLimit(string name)
    {
        var result = Cases[name].WithValidRows(ImportLimits.MaxRows);

        _ = result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Validate_ShouldFail_WhenRowCountExceedsLimit(string name)
    {
        var result = Cases[name].WithValidRows(ImportLimits.MaxRows + 1);

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Rows");
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Validate_ShouldFail_WhenThereAreNoRows(string name)
    {
        var result = Cases[name].WithValidRows(0);

        _ = result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Rows");
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Validate_ShouldFail_WhenARowFieldExceedsMaxLength(string name)
    {
        var testCase = Cases[name];

        var result = testCase.WithTooLongField();

        _ = result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be($"Rows[0].{testCase.TooLongProperty}");
    }

    private static Case For<TCommand, TRow>(
        IValidator<TCommand> validator,
        Func<List<TRow>, TCommand> build,
        TRow validRow,
        TRow tooLongRow,
        string tooLongProperty) =>
        new(
            count => validator.Validate(build([.. Enumerable.Repeat(validRow, count)])),
            () => validator.Validate(build([tooLongRow])),
            tooLongProperty);

    private static string Long(int length) => new('X', length);
}
