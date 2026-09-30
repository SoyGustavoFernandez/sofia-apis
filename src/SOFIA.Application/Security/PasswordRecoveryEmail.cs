using System.Net;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Security;

public static class PasswordRecoveryEmail
{
    public const string Subject = "Restablece tu contraseña de SOFIA";

    private static readonly Lazy<string> HtmlTemplate = new(() => ReadTemplate("PasswordRecovery.html"));
    private static readonly Lazy<string> TextTemplate = new(() => ReadTemplate("PasswordRecovery.txt"));

    public static EmailMessage Build(string to, string nombre, string nombreUsuario, string resetLink)
    {
        // Every value is HTML-encoded in the HTML part, so a crafted employee name cannot inject markup
        var html = HtmlTemplate.Value
            .Replace("{{NOMBRE}}", WebUtility.HtmlEncode(nombre), StringComparison.Ordinal)
            .Replace("{{USUARIO}}", WebUtility.HtmlEncode(nombreUsuario), StringComparison.Ordinal)
            .Replace("{{ENLACE}}", WebUtility.HtmlEncode(resetLink), StringComparison.Ordinal);

        var text = TextTemplate.Value
            .Replace("{{NOMBRE}}", nombre, StringComparison.Ordinal)
            .Replace("{{USUARIO}}", nombreUsuario, StringComparison.Ordinal)
            .Replace("{{ENLACE}}", resetLink, StringComparison.Ordinal);

        return new EmailMessage(to, Subject, html, text);
    }

    private static string ReadTemplate(string fileName)
    {
        var assembly = typeof(PasswordRecoveryEmail).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith($".Security.Templates.{fileName}", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
