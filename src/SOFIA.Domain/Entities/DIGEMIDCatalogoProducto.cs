using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public class DigemidCatalogoProducto : BaseEntity
{
    public string CodProd { get; private set; } = null!;
    public string NomProd { get; private set; } = null!;
    public string? Concent { get; private set; }
    public string? FormaFarmaceutica { get; private set; }
    public string? Fraccion { get; private set; }
    public string? RegistroSanitario { get; private set; }
    public string? Titular { get; private set; }
    public string Estado { get; private set; } = null!;

    private DigemidCatalogoProducto() { } // EF Core

    public static Result<DigemidCatalogoProducto> Create(
        string codProd,
        string nomProd,
        string? concent,
        string? formaFarmaceutica,
        string? fraccion,
        string? registroSanitario,
        string? titular,
        string estado)
    {
        if (string.IsNullOrWhiteSpace(codProd))
        {
            return Result.Failure<DigemidCatalogoProducto>(Error.Validation("DIGEMID.CodProd", "CodProd is required."));
        }

        if (string.IsNullOrWhiteSpace(nomProd))
        {
            return Result.Failure<DigemidCatalogoProducto>(Error.Validation("DIGEMID.NomProd", "NomProd is required."));
        }

        return Result.Success(new DigemidCatalogoProducto
        {
            CodProd = codProd,
            NomProd = nomProd,
            Concent = concent,
            FormaFarmaceutica = formaFarmaceutica,
            Fraccion = fraccion,
            RegistroSanitario = registroSanitario,
            Titular = titular,
            Estado = estado
        });
    }

    public Result Update(string codProd, string nomProd, string? concent, string? formaFarmaceutica, string? fraccion, string? registroSanitario, string? titular, string estado)
    {
        if (string.IsNullOrWhiteSpace(codProd))
        {
            return Result.Failure(Error.Validation("DIGEMID.CodProd", "CodProd is required."));
        }

        if (string.IsNullOrWhiteSpace(nomProd))
        {
            return Result.Failure(Error.Validation("DIGEMID.NomProd", "NomProd is required."));
        }

        CodProd = codProd; NomProd = nomProd; Concent = concent; FormaFarmaceutica = formaFarmaceutica;
        Fraccion = fraccion; RegistroSanitario = registroSanitario; Titular = titular; Estado = estado;
        return Result.Success();
    }
}
