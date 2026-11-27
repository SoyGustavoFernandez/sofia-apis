using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.CatalogoServicios.Commands.CreateServicioCatalogo;

public record CreateServicioCatalogoCommand(string Nombre, decimal Precio, bool AplicaMedicamento = false, bool RegistraResultado = false) : ICommand<Guid>;
