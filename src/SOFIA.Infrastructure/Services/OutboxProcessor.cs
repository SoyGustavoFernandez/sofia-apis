using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Services;

public class OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger) : BackgroundService
{
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
                // Simulamos la publicación del evento en la consola/bus
                logger.LogInformation("Publishing event of type '{Type}' with payload: {Payload}", outboxEvent.TipoEvento, outboxEvent.PayloadJson);

                // Marcar como procesado
                _ = outboxEvent.Update(
                    outboxEvent.TipoEvento,
                    outboxEvent.PayloadJson,
                    true,
                    DateTime.UtcNow,
                    null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox event {Id}.", outboxEvent.Id);
                _ = outboxEvent.Update(
                    outboxEvent.TipoEvento,
                    outboxEvent.PayloadJson,
                    false,
                    null,
                    ex.Message);
            }
        }

        _ = await dbContext.SaveChangesAsync(stoppingToken);
    }
}
