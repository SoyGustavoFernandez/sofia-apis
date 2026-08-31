using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Delete;

public record DeleteFormulacionClinicaCommand(Guid Id) : ICommand;
