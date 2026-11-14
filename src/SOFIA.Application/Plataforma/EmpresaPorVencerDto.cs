using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma;

public record EmpresaPorVencerDto(Guid Id, string Nombre, EstadoEmpresa Estado, DateTimeOffset FechaVencimiento);
