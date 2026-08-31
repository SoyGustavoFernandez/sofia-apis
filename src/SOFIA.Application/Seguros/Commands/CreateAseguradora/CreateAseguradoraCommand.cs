using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Commands.CreateAseguradora;

public record CreateAseguradoraCommand(string NombreComercial, string CodigoIdentificadorNacional) : ICommand<Guid>;
