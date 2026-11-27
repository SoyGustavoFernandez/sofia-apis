using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.CatalogoServicios.Commands.DeleteServicioCatalogo;

public record DeleteServicioCatalogoCommand(Guid Id) : ICommand;
