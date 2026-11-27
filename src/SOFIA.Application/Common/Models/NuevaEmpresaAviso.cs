namespace SOFIA.Application.Common.Models;

// What the platform owner is told about a new company: never its username or password
public record NuevaEmpresaAviso(string NombreEmpresa, string Ruc, string AdminNombre, string AdminEmail, DateTimeOffset FechaRegistro);
