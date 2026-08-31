using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.UpdateDigemidProducto;

public record UpdateDigemidProductoCommand(Guid Id, string CodProd, string NomProd, string? Concent, string? FormaFarmaceutica, string? Fraccion, string? RegistroSanitario, string? Titular, string Estado) : ICommand;
