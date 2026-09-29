using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Security.Commands.ChangePassword;

public record ChangePasswordCommand(Guid CuentaId, string CurrentPassword, string NewPassword) : ICommand;
