using SOFIA.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SOFIA.Application.Delivery.Queries.GetDespachoDeliveryById;

public record GetDespachoDeliveryByIdQuery(Guid Id) : IRequest<Result<DespachoDeliveryDto>>;

public record DespachoDeliveryDto(Guid Id);
