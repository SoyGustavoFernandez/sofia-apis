using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Empresas.Commands.DeleteEmpresa;

public record DeleteEmpresaCommand(Guid Id) : ICommand;
