using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Profesionales.Commands.CargaMasivaProfesionalesSalud;

public record ProfesionalSaludImportRow(string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica);
public record CargaMasivaProfesionalesSaludCommand(List<ProfesionalSaludImportRow> Rows) : ICommand<int>;
