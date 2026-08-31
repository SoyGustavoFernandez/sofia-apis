using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.DIGEMID.Commands.CreateDigemidProducto;

public record CreateDigemidProductoCommand(string CodProd, string NomProd, string? Concent, string? FormaFarmaceutica, string? Fraccion, string? RegistroSanitario, string? Titular, string Estado) : ICommand<Guid>;
