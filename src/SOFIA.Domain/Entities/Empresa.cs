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

    public bool EstaVigente =>
        Estado == EstadoEmpresa.Activo ||
        (Estado == EstadoEmpresa.TrialActivo && FechaVencimiento > DateTimeOffset.UtcNow);

    public ICollection<Sucursal> Sucursales { get; private set; } = [];

    private const int DiasTrial = 30;

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
            FechaVencimiento = ahora.AddDays(DiasTrial)
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

    public void ActivarSuscripcion(int diasVigencia = 365)
    {
        Estado = EstadoEmpresa.Activo;
        FechaVencimiento = DateTimeOffset.UtcNow.AddDays(diasVigencia);
    }

    public void Suspender() => Estado = EstadoEmpresa.Suspendido;

    public void Cancelar() => Estado = EstadoEmpresa.Cancelado;

    public void ExtenderTrial(int diasExtra)
    {
        if (Estado == EstadoEmpresa.TrialActivo)
        {
            FechaVencimiento = FechaVencimiento.AddDays(diasExtra);
        }
    }
}
