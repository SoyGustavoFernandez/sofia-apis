using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.CatalogoServicios.Commands.UpdateServicioCatalogo;

public record UpdateServicioCatalogoCommand(Guid Id, string Nombre, decimal Precio, bool AplicaMedicamento = false, bool RegistraResultado = false) : ICommand;
