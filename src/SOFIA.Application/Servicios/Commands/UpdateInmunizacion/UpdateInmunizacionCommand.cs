using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SOFIA.Application.Servicios.Commands.UpdateInmunizacion;

public record UpdateInmunizacionCommand(Guid Id) : ICommand<Guid>; // TODO: Add properties manually
