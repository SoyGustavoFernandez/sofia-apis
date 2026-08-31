using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;

public record CreateUnidadMedidaCommand(string Codigo, string Descripcion) : ICommand<Guid>;
