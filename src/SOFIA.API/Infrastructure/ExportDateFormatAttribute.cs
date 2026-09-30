using System.ComponentModel.DataAnnotations;

namespace SOFIA.API.Infrastructure;

// Only the formats the SPA sends are accepted, so a client-supplied pattern can never break or bloat DateTime.ToString
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class ExportDateFormatAttribute : ValidationAttribute
{
    public static readonly string[] AllowedFormats = ["dd/MM/yyyy", "MM/dd/yyyy", "dd/MM/yyyy HH:mm", "MM/dd/yyyy HH:mm"];

    public ExportDateFormatAttribute() : base("Export.DateFormat.Invalid") { }

    public override bool IsValid(object? value) => value is string format && AllowedFormats.Contains(format, StringComparer.Ordinal);
}
