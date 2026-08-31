using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.Application.Security.Commands.Login;

public record LoginCommand(string NombreUsuario, string Password) : ICommand<LoginResult>;
