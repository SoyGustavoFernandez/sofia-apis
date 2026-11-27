using System.Globalization;
using System.Net;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Empresas.Notifications;

public static class NuevaEmpresaEmail
{
    private static readonly TimeSpan Peru = TimeSpan.FromHours(-5);

    public static EmailMessage Build(string to, NuevaEmpresaAviso aviso)
    {
        (string Etiqueta, string Valor)[] datos =
        [
            ("Empresa", aviso.NombreEmpresa),
            ("RUC", aviso.Ruc),
            ("Administrador", aviso.AdminNombre),
            ("Correo", aviso.AdminEmail),
            ("Registro", aviso.FechaRegistro.ToOffset(Peru).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " (hora de Perú)"),
        ];

        // The values come from an anonymous form, so they are encoded in HTML and flattened in the subject
        var filas = string.Concat(datos.Select(d => $"<tr><td><strong>{d.Etiqueta}</strong></td><td>{WebUtility.HtmlEncode(d.Valor)}</td></tr>"));
        var html = $"<p>Se registró una empresa nueva en SOFIA.</p><table cellpadding=\"4\">{filas}</table>";
        var text = "Se registró una empresa nueva en SOFIA.\n\n" + string.Join('\n', datos.Select(d => $"{d.Etiqueta}: {d.Valor}"));
        var nombre = aviso.NombreEmpresa.ReplaceLineEndings(" ");

        return new EmailMessage(to, $"Nueva empresa en SOFIA: {nombre}", html, text);
    }
}
