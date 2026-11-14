using SOFIA.Domain.Common;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Domain.Entities;

public enum EstadoEmpresa
{
    TrialActivo = 0,
    Activo = 1,
    Suspendido = 2,
    Cancelado = 3
}

public sealed class Empresa : BaseEntity
{
    private Empresa() { }

    public string Nombre { get; private set; } = string.Empty;
    public Ruc? RUC { get; private set; }
    public EstadoEmpresa Estado { get; private set; }
    public DateTimeOffset FechaInicioTrial { get; private set; }
    public DateTimeOffset FechaVencimiento { get; private set; }

    // Off by default and changed only in the database: when on, simple-prescription products also need a registered prescription
    public bool ExigirRegistroReceta { get; private set; }

    public bool EstaVigente => EstaVigenteEn(DateTimeOffset.UtcNow);

    // Single subscription rule shared by the DTOs, login, token refresh and every authenticated request
    public bool EstaVigenteEn(DateTimeOffset ahora) =>
        !IsDeleted &&
        (Estado == EstadoEmpresa.Activo || Estado == EstadoEmpresa.TrialActivo) &&
        FechaVencimiento > ahora;

    public ICollection<Sucursal> Sucursales { get; private set; } = [];

    private const int DiasTrial = 30;

    private static readonly Error FechaVencimientoPasadaError =
        Error.Validation("Empresa.Suscripcion.FechaVencimiento", "The expiry date must be in the future.");

    public static Result<Empresa> Create(string nombre, string? ruc = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Result.Failure<Empresa>(Error.Validation("Empresa.Nombre", "Nombre is required."));
        }

        Ruc? rucVo = null;
        if (ruc is not null)
        {
            var rucResult = Ruc.Create(ruc);
            if (rucResult.IsFailure)
            {
                return Result.Failure<Empresa>(rucResult.Error);
            }
            rucVo = rucResult.Value;
        }

        var ahora = DateTimeOffset.UtcNow;
        var empresa = new Empresa
        {
            Nombre = nombre,
            RUC = rucVo,
            Estado = EstadoEmpresa.TrialActivo,
            FechaInicioTrial = ahora,
            FechaVencimiento = ahora.AddDays(DiasTrial),
            ExigirRegistroReceta = false
        };
        empresa.TenantId = empresa.Id;
        return Result.Success(empresa);
    }

    public Result Update(string nombre, string? ruc)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Result.Failure(Error.Validation("Empresa.Nombre", "Nombre is required."));
        }

        Ruc? rucVo = null;
        if (ruc is not null)
        {
            var rucResult = Ruc.Create(ruc);
            if (rucResult.IsFailure)
            {
                return Result.Failure(rucResult.Error);
            }
            rucVo = rucResult.Value;
        }

        Nombre = nombre;
        RUC = rucVo;
        return Result.Success();
    }

    // A paid subscription is granted up to a date and expires on its own, exactly like the trial
    public Result ActivarSuscripcion(DateTimeOffset fechaVencimiento)
    {
        if (fechaVencimiento <= DateTimeOffset.UtcNow)
        {
            return Result.Failure(FechaVencimientoPasadaError);
        }

        Estado = EstadoEmpresa.Activo;
        FechaVencimiento = fechaVencimiento;
        return Result.Success();
    }

    public Result ExtenderTrial(DateTimeOffset fechaVencimiento)
    {
        if (Estado != EstadoEmpresa.TrialActivo)
        {
            return Result.Failure(Error.Conflict("Empresa.Suscripcion.NoEstaEnPrueba", "Only a company on trial can have its trial extended."));
        }

        if (fechaVencimiento <= DateTimeOffset.UtcNow)
        {
            return Result.Failure(FechaVencimientoPasadaError);
        }

        FechaVencimiento = fechaVencimiento;
        return Result.Success();
    }

    public void Suspender() => Estado = EstadoEmpresa.Suspendido;

    public void Cancelar() => Estado = EstadoEmpresa.Cancelado;
}
