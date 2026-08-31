using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.ResetPassword;

public record ResetPasswordCommand(string NombreUsuario, string Token, string NewPassword) : ICommand;
