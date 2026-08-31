using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Transferencias.Commands.RecibirTransferencia;

public record RecepcionLoteInputDto(Guid LoteId, decimal CantidadRecibida);

public record RecibirTransferenciaCommand(
    Guid Id,
    List<RecepcionLoteInputDto> Recepciones) : ICommand;
