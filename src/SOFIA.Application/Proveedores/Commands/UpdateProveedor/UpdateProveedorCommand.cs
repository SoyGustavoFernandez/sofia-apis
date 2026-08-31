using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.UpdateProveedor;

public record UpdateProveedorCommand(Guid Id, string RazonSocial, string TaxId, string? TerminosFinancieros, decimal? CalificacionEsg, decimal TasaCumplimiento) : ICommand;
