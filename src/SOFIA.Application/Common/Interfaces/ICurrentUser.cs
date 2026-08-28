namespace SOFIA.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? Id { get; }
    string? Name { get; }
    string? SucursalId { get; }
    string? EmpresaId { get; }
    bool IsAuthenticated { get; }
}
