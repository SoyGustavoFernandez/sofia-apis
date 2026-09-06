using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Medicamentos.Commands.CargaMasivaMedicamentos;

public record MedicamentoImportRow(
    string CodigoNacional,
    string NombreComercial,
    string Laboratorio,
    string UnidadBase,
    string CondicionVenta);

public record CargaMasivaMedicamentosCommand(List<MedicamentoImportRow> Rows) : ICommand<int>;
