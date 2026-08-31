using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Transferencias.Commands.CancelarTransferencia;

public record CancelarTransferenciaCommand(Guid Id) : ICommand;
