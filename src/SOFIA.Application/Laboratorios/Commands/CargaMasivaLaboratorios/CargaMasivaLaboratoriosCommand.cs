using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Laboratorios.Commands.CargaMasivaLaboratorios;

public record LaboratorioImportRow(string NombreCompania, string? CodigoIdentificador);
public record CargaMasivaLaboratoriosCommand(List<LaboratorioImportRow> Rows) : ICommand<int>;
