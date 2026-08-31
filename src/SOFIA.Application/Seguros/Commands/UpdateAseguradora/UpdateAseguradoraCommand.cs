using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Commands.UpdateAseguradora;

public record UpdateAseguradoraCommand(Guid Id, string NombreComercial, string CodigoIdentificadorNacional) : ICommand;
