using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Plataforma.Notifications;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma.Commands.EnviarAvisosVencimiento;

public class EnviarAvisosVencimientoCommandHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    IPlatformNotificationSettings settings) : IRequestHandler<EnviarAvisosVencimientoCommand, Result<AvisosVencimientoResultDto>>
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);

    private sealed record Destino(Guid Id, string Nombre, string Ruc, EstadoEmpresa Estado, DateTimeOffset Vencimiento, int DiasRestantes, List<string> Correos);

    public async Task<Result<AvisosVencimientoResultDto>> Handle(EnviarAvisosVencimientoCommand request, CancellationToken cancellationToken)
    {
        var ahora = DateTimeOffset.UtcNow;
        var porVencer = await EmpresasPorVencerAsync(ahora, cancellationToken);

        var pago = await context.PlataformaConfiguracionesPago.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var enviados = 0;
        var fallidos = 0;
        foreach (var empresa in porVencer)
        {
            var hito = AvisoEnviado.HitoPara(empresa.DiasRestantes);
            if (hito is null || empresa.Correos.Count == 0 || await YaEnviadoAsync(AvisoEnviado.DeEmpresa(empresa.Id, empresa.Vencimiento, hito.Value), cancellationToken))
            {
                continue;
            }

            var entregado = false;
            foreach (var correo in empresa.Correos)
            {
                entregado |= await EnviarAsync(AvisoVencimientoEmail.Build(correo, empresa.Nombre, empresa.Estado, empresa.Vencimiento, empresa.DiasRestantes, pago));
            }

            // Recorded only once delivered, so a failed one is tried again on the next run
            if (entregado)
            {
                await RegistrarAsync(AvisoEnviado.DeEmpresa(empresa.Id, empresa.Vencimiento, hito.Value));
                enviados++;
            }
            else
            {
                fallidos++;
            }
        }

        var resumenEnviado = false;
        if (porVencer.Count > 0 && !string.IsNullOrWhiteSpace(settings.OwnerRecipient) && !await YaEnviadoAsync(AvisoEnviado.ResumenDelDia(ahora), cancellationToken))
        {
            var filas = porVencer
                .Select(e => new EmpresaPorVencerResumen(e.Nombre, e.Ruc, string.Join(", ", e.Correos), e.Estado, e.Vencimiento, e.DiasRestantes))
                .ToList();
            resumenEnviado = await EnviarAsync(ResumenVencimientosEmail.Build(settings.OwnerRecipient, filas));
            if (resumenEnviado)
            {
                await RegistrarAsync(AvisoEnviado.ResumenDelDia(ahora));
            }
            else
            {
                fallidos++;
            }
        }

        return Result.Success(new AvisosVencimientoResultDto(enviados, resumenEnviado, fallidos));
    }

    // Reads every company: there is no signed-in tenant in a scheduled run
    private async Task<List<Destino>> EmpresasPorVencerAsync(DateTimeOffset ahora, CancellationToken cancellationToken)
    {
        var limite = ahora.AddDays(AvisoEnviado.DiasPrimerAviso + 1);
        var empresas = await context.Empresas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .AsNoTracking()
            .Where(e => (e.Estado == EstadoEmpresa.TrialActivo || e.Estado == EstadoEmpresa.Activo) && e.FechaVencimiento > ahora && e.FechaVencimiento <= limite)
            .OrderBy(e => e.FechaVencimiento)
            .ThenBy(e => e.Nombre)
            .ToListAsync(cancellationToken);

        var ids = empresas.Select(e => (Guid?)e.Id).ToList();
        var cuentas = await context.Cuentas
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .AsNoTracking()
            .Include(c => c.Empleado)
            .Include(c => c.Roles)
            .Where(c => ids.Contains(c.TenantId) && c.CuentaActiva)
            .ToListAsync(cancellationToken);

        return [.. empresas
            .Select(e => new Destino(
                e.Id,
                e.Nombre,
                e.RUC?.Value ?? "-",
                e.Estado,
                e.FechaVencimiento,
                AvisoEnviado.DiasRestantes(e.FechaVencimiento, ahora),
                [.. cuentas
                    .Where(c => c.TenantId == e.Id && c.EsAdmin && c.Empleado is { IsDeleted: false, Email: not null })
                    .Select(c => c.Empleado!.Email!)
                    .Distinct()]))
            .Where(d => d.DiasRestantes <= AvisoEnviado.DiasPrimerAviso)];
    }

    private Task<bool> YaEnviadoAsync(AvisoEnviado aviso, CancellationToken cancellationToken) =>
        context.AvisosEnviados
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .AnyAsync(a => a.EmpresaId == aviso.EmpresaId && a.Fecha == aviso.Fecha && a.Dias == aviso.Dias, cancellationToken);

    // Saved one by one, and even if the caller gave up: the mail is already gone
    private async Task RegistrarAsync(AvisoEnviado aviso)
    {
        _ = context.AvisosEnviados.Add(aviso);
        _ = await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<bool> EnviarAsync(EmailMessage mensaje)
    {
        try
        {
            using var limite = new CancellationTokenSource(SendTimeout);
            return (await emailSender.SendAsync(mensaje, limite.Token)).IsSuccess;
        }
        catch (Exception ex) when (ex is OperationCanceledException or FormatException)
        {
            return false;
        }
    }
}
