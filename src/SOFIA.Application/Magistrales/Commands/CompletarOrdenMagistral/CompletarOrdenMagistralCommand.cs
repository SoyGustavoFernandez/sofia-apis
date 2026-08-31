using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Magistrales.Commands.CompletarOrdenMagistral;

public record CompletarOrdenMagistralCommand(
    Guid OrdenId,
    string NumeroLoteMfr,
    DateTimeOffset FechaCaducidad
) : ICommand<Guid>;
