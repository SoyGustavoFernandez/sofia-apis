using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Transferencias.Commands.CreateTransferencia;

public record CreateTransferenciaDetailDto(Guid LoteId, decimal CantidadEnviada);

public record CreateTransferenciaCommand(
    Guid SucursalDestinoId,
    List<CreateTransferenciaDetailDto> Detalles) : ICommand<Guid>;
