using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.GenerarActaDestruccion;

public record GenerarActaDestruccionCommand(string NumeroResolucionInterna, string EmpresaResiduosBiocontaminados, string? ManifiestoTransporteDoc, DateTime FechaEjecucion, Guid RegenteResponsableId, string? RutaActaFirmadaPdf) : ICommand<Guid>;
