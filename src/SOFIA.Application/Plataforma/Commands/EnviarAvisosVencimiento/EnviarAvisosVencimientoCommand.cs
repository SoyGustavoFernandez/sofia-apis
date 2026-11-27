using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Plataforma.Commands.EnviarAvisosVencimiento;

// Run once a day by the scheduled task; safe to repeat
public record EnviarAvisosVencimientoCommand : ICommand<AvisosVencimientoResultDto>;
