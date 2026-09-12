using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Create;

public record CreateFormulacionClinicaCommand : ICommand<Guid>
{
    public Guid ProductoId { get; init; }
    public Guid IngredienteId { get; init; }
    public decimal ConcentracionDosis { get; init; }
    public Guid UnidadMedidaId { get; init; }
    public string? CodigoTeOrange { get; init; }
}
