using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Auditoria.Commands.AnonimizarDatos;

public record AnonimizarDatosCommand(string Tabla, Guid RegistroId) : ICommand<Guid>;
