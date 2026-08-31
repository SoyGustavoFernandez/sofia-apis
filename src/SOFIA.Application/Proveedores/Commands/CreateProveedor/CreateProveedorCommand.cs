using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.CreateProveedor;

public record CreateProveedorCommand(string RazonSocial, string TaxId, string? TerminosFinancieros, decimal? CalificacionEsg, decimal TasaCumplimiento = 100.00m) : ICommand<Guid>;
