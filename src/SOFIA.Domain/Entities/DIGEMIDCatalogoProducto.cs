using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public class DIGEMIDCatalogoProducto : BaseEntity
{
    public string CodProd { get; private set; } = null!;
    public string NomProd { get; private set; } = null!;
    public string? Concent { get; private set; }
    public string? FormaFarmaceutica { get; private set; }
    public string? Fraccion { get; private set; }
    public string? RegistroSanitario { get; private set; }
    public string? Titular { get; private set; }
    public string Estado { get; private set; } = null!;

    private DIGEMIDCatalogoProducto() { } // EF Core

    public static Result<DIGEMIDCatalogoProducto> Create(
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
            return Result.Failure<DIGEMIDCatalogoProducto>(Error.Validation("DIGEMID.CodProd", "CodProd is required."));
        }

        if (string.IsNullOrWhiteSpace(nomProd))
        {
            return Result.Failure<DIGEMIDCatalogoProducto>(Error.Validation("DIGEMID.NomProd", "NomProd is required."));
        }

        return Result.Success(new DIGEMIDCatalogoProducto
        {
            Id = Guid.NewGuid(),
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
}
