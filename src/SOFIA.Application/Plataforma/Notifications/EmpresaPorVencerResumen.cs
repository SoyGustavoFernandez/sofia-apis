using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma.Notifications;

public record EmpresaPorVencerResumen(string Nombre, string Ruc, string Correos, EstadoEmpresa Estado, DateTimeOffset Vencimiento, int DiasRestantes);
