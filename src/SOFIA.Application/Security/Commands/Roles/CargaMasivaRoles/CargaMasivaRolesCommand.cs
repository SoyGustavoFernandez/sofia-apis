using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Security.Commands.Roles.CargaMasivaRoles;

public record RolImportRow(string NombreRol, string? Descripcion, int NivelJerarquia);
public record CargaMasivaRolesCommand(List<RolImportRow> Rows) : ICommand<int>;
