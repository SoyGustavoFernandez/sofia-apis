using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.CerrarCaja;

public record CerrarCajaCommand(Guid SesionId, decimal MontoCierre) : ICommand<Guid>;
