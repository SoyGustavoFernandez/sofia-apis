namespace SOFIA.Application.CatalogoServicios.Common;

public record ServicioCatalogoDto(Guid Id, string Nombre, decimal Precio, bool AplicaMedicamento, bool RegistraResultado);
