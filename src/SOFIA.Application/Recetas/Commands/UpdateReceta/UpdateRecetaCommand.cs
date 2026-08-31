using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Recetas.Commands.UpdateReceta;

public record UpdateRecetaCommand(Guid Id) : ICommand<Guid>; // TODO: Add properties manually
