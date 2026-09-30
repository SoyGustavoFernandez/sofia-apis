using System.ComponentModel.DataAnnotations;

namespace SOFIA.API.Infrastructure;

// Caps the client-supplied column titles so an export request cannot amplify memory use
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class ExportHeadersAttribute : ValidationAttribute
{
    public const int MaxHeaders = 50;
    public const int MaxHeaderLength = 100;

    public ExportHeadersAttribute() : base("Export.Headers.Invalid") { }

    public override bool IsValid(object? value) =>
        value is string[] headers
        && headers.Length <= MaxHeaders
        && Array.TrueForAll(headers, h => h is not null && h.Length <= MaxHeaderLength);
}
