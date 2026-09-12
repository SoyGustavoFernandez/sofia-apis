using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Update;

public record UpdateFormulacionClinicaCommand : ICommand
{
    public Guid Id { get; init; }
    public Guid IngredienteId { get; init; }
    public decimal ConcentracionDosis { get; init; }
    public Guid UnidadMedidaId { get; init; }
    public string? CodigoTeOrange { get; init; }
}
