namespace SOFIA.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? Id { get; }
    string? CuentaId { get; }
    string? Name { get; }
    string? SucursalId { get; }
    string? EmpresaId { get; }
    bool IsAuthenticated { get; }
    string? ClientIpAddress { get; }
    bool IsInRole(string roleName);
}
