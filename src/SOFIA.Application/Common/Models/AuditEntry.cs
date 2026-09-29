namespace SOFIA.Application.Common.Models;

/// <summary>Security audit event produced by an IAuditableCommand; Detalle is a short non-PII summary.</summary>
public sealed record AuditEntry(string Evento, string Tabla, Guid RegistroId, string? Detalle = null);
