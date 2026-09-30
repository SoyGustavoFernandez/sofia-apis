using System.ComponentModel.DataAnnotations;

namespace SOFIA.API.Infrastructure;

// Caps a client-supplied cell label repeated on every exported row so it cannot amplify memory use
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class ExportLabelAttribute : ValidationAttribute
{
    public const int MaxLength = 20;

    public ExportLabelAttribute() : base("Export.Label.Invalid") { }

    public override bool IsValid(object? value) => value is string label && label.Length <= MaxLength;
}
