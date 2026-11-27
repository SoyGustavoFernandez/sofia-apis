using System.Globalization;
using System.Net;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma.Notifications;

public static class ResumenVencimientosEmail
{
    public static EmailMessage Build(string to, IReadOnlyList<EmpresaPorVencerResumen> empresas)
    {
        var lineas = empresas.Select(e =>
        {
            var plan = e.Estado == EstadoEmpresa.TrialActivo ? "prueba" : "suscripción";
            var fecha = AvisoEnviado.DiaEnPeru(e.Vencimiento).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            return $"{e.Nombre} (RUC {e.Ruc}) - {plan} - vence el {fecha}, faltan {e.DiasRestantes} días - {e.Correos}";
        }).ToList();

        var titulo = $"Empresas que vencen en los próximos {AvisoEnviado.DiasPrimerAviso} días: {empresas.Count}";
        var html = $"<p>{WebUtility.HtmlEncode(titulo)}</p><ul>{string.Concat(lineas.Select(l => $"<li>{WebUtility.HtmlEncode(l)}</li>"))}</ul>";

        return new EmailMessage(to, $"SOFIA: {empresas.Count} empresas por vencer", html, $"{titulo}\n\n{string.Join('\n', lineas)}");
    }
}
