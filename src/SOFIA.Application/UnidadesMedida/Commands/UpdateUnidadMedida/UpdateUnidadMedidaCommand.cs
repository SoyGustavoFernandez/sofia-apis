using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;

public record UpdateUnidadMedidaCommand(Guid Id, string Codigo, string Descripcion) : ICommand;
