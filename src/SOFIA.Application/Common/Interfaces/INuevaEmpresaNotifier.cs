using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Common.Interfaces;

public interface INuevaEmpresaNotifier
{
    // Best effort: it never throws, so a mail problem cannot undo or fail a registration
    Task NotificarAsync(NuevaEmpresaAviso aviso);
}
