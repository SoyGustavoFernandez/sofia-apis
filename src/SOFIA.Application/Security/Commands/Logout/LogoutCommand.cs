using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Security.Commands.Logout;

// Either source identifies the session: the bearer's account, or the refresh cookie when the access token already expired
public record LogoutCommand(Guid? CuentaId = null, string? RefreshToken = null) : ICommand;
