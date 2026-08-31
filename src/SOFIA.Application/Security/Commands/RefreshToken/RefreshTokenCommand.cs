using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Domain.Common;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.Application.Security.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : ICommand<LoginResult>;
