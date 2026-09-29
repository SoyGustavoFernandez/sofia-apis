using SOFIA.Application.Common.Behaviors;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Common.Interfaces;

/// <summary>Command whose success is recorded as a security audit event by AuditBehavior, inside its transaction.</summary>
public interface IAuditableCommand : IBaseCommand
{
    /// <summary>Builds the audit event from the command and its success value (null skips the audit); never include PII or secrets.</summary>
    AuditEntry? GetAuditEntry(object? resultValue);
}
