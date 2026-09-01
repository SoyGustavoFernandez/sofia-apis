using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.DIGEMID.Commands.CargaMasivaDigemid;

public record DigemidImportRow(
    string CodProd,
    string NomProd,
    string? Concent,
    string? FormaFarmaceutica,
    string? Fraccion,
    string? RegistroSanitario,
    string? Titular,
    string Estado);

public record CargaMasivaDigemidCommand(List<DigemidImportRow> Rows) : ICommand<int>;
