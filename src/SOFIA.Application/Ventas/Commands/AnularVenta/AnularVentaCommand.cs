using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Commands.AnularVenta;

public record AnularVentaCommand(Guid VentaId, string Motivo) : ICommand;
