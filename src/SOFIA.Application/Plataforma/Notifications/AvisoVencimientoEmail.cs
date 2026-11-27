using System.Globalization;
using System.Net;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma.Notifications;

public static class AvisoVencimientoEmail
{
    public static EmailMessage Build(string to, string empresa, EstadoEmpresa estado, DateTimeOffset vencimiento, int diasRestantes, PlataformaConfiguracionPago? pago)
    {
        var plan = estado == EstadoEmpresa.TrialActivo ? "prueba" : "suscripción";
        var cuando = diasRestantes switch
        {
            0 => "hoy",
            1 => "mañana",
            _ => $"en {diasRestantes} días"
        };
        var fecha = AvisoEnviado.DiaEnPeru(vencimiento).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        List<string> parrafos =
        [
            $"Hola, {empresa}:",
            $"Tu {plan} de SOFIA vence {cuando}, el {fecha}.",
            ComoPagar(pago),
        ];
        if (!string.IsNullOrWhiteSpace(pago?.Instrucciones))
        {
            parrafos.Add(pago.Instrucciones);
        }

        // The company name comes from an anonymous form, so everything is encoded in the HTML part
        var html = string.Concat(parrafos.Select(p => $"<p>{WebUtility.HtmlEncode(p)}</p>"));

        return new EmailMessage(to, $"Tu {plan} de SOFIA vence {cuando}", html, string.Join("\n\n", parrafos));
    }

    private static string ComoPagar(PlataformaConfiguracionPago? pago)
    {
        if (pago is null)
        {
            return "Para seguir usando el sistema, escríbenos y la renovamos.";
        }

        var yape = string.IsNullOrWhiteSpace(pago.YapeNumero) ? "paga por Yape" : $"paga por Yape al {pago.YapeNumero}";
        return $"Para seguir usando el sistema, {yape} y envía la captura por WhatsApp al {pago.WhatsappNumero}.";
    }
}
