using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas;

public record EmpresaDto(
    Guid Id,
    string Nombre,
    string? RUC,
    EstadoEmpresa Estado,
    DateTimeOffset FechaInicioTrial,
    DateTimeOffset FechaVencimiento,
    bool EstaVigente,
    int CantidadSucursales
);
