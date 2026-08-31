using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;

public record RegistrarPrecioProveedorCommand(Guid ProveedorId, Guid MedicamentoId, decimal PrecioCompra, decimal DescuentoPorcentaje) : ICommand<Guid>;
