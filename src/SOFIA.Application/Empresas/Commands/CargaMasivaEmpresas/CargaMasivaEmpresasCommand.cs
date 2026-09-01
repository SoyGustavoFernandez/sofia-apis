using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Empresas.Commands.CargaMasivaEmpresas;

public record EmpresaImportRow(string Nombre, string? RUC);
public record CargaMasivaEmpresasCommand(List<EmpresaImportRow> Rows) : ICommand<int>;
