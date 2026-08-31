using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Transferencias.Commands.DespacharTransferencia;

public record DespacharTransferenciaCommand(Guid Id) : ICommand;
