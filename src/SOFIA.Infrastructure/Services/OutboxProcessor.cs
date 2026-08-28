using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;
using System.Text.Json;

namespace SOFIA.Infrastructure.Services;

public class OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger) : BackgroundService
{
    // Cache of event types to avoid repeated reflection
    private static readonly Dictionary<string, Type?> _typeCache = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor background service starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing outbox events.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var events = await dbContext.SistemaOutboxEventos
            .Where(e => !e.Procesado && e.ErrorPublicacion == null && !e.IsDeleted)
            .OrderBy(e => e.FechaCreacion)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (events.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {Count} pending outbox events.", events.Count);

        foreach (var outboxEvent in events)
        {
            try
            {
                await DispatchEventAsync(publisher, outboxEvent.TipoEvento, outboxEvent.PayloadJson, stoppingToken);

                _ = outboxEvent.Update(
                    outboxEvent.TipoEvento,
                    outboxEvent.PayloadJson,
                    true,
                    DateTime.UtcNow,
                    null);

                logger.LogInformation("Dispatched outbox event {Id} of type '{Type}'.", outboxEvent.Id, outboxEvent.TipoEvento);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch outbox event {Id} of type '{Type}'.", outboxEvent.Id, outboxEvent.TipoEvento);
                _ = outboxEvent.Update(
                    outboxEvent.TipoEvento,
                    outboxEvent.PayloadJson,
                    false,
                    null,
                    ex.Message[..Math.Min(ex.Message.Length, 500)]);
            }
        }

        _ = await dbContext.SaveChangesAsync(stoppingToken);
    }

    private async Task DispatchEventAsync(IPublisher publisher, string tipoEvento, string payloadJson, CancellationToken ct)
    {
        if (!_typeCache.TryGetValue(tipoEvento, out var eventType))
        {
            // Busca el tipo en todos los assemblies cargados
            eventType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(tipoEvento))
                .FirstOrDefault(t => t is not null);

            _typeCache[tipoEvento] = eventType;
        }

        if (eventType is null)
        {
            logger.LogWarning("Outbox event type '{Type}' not found. Skipping.", tipoEvento);
            return;
        }

        if (!typeof(INotification).IsAssignableFrom(eventType))
        {
            logger.LogWarning("El tipo '{Type}' no implementa INotification. Se omite.", tipoEvento);
            return;
        }

        var notification = (INotification?)JsonSerializer.Deserialize(payloadJson, eventType)
            ?? throw new InvalidOperationException($"No se pudo deserializar el payload para '{tipoEvento}'.");

        await publisher.Publish(notification, ct);
    }
}
