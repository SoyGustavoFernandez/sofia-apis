using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Seguros.Commands.DeleteAseguradora;

public record DeleteAseguradoraCommand(Guid Id) : ICommand<Guid>;
