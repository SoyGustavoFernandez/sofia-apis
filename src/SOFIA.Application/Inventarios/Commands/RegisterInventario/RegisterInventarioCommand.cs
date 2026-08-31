using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Inventarios.Commands.RegisterInventario;

public record RegisterInventarioCommand(
    Guid SucursalId,
    Guid LoteId,
    decimal Cantidad,
    bool EsAjusteDirecto = false) : ICommand<Guid>;
