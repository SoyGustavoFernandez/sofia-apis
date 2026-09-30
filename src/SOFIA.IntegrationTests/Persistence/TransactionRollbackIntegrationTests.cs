using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Persistence;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Persistence;

public class TransactionRollbackIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Command_ShouldPersistNothing_WhenItThrowsAfterSavingChanges()
    {
        var nombre = $"Rollback {Guid.NewGuid():N}"[..30];
        var host = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddTransient<IRequestHandler<FailAfterWriteCommand, Result<Guid>>, FailAfterWriteCommandHandler>()));

        using (var scope = host.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var act = () => sender.Send(new FailAfterWriteCommand(nombre));

            _ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(FailAfterWriteCommandHandler.Message);
        }

        using var verifyScope = host.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await db.Laboratorios.IgnoreQueryFilters().AnyAsync(l => l.NombreCompania == nombre);
        _ = persisted.Should().BeFalse(because: "TransactionBehavior must roll back the write that SaveChanges already sent");
    }
}

// Test-only command: its handler saves a row and then fails, which only a real transaction can undo
public record FailAfterWriteCommand(string NombreCompania) : ICommand<Guid>;

public class FailAfterWriteCommandHandler(IApplicationDbContext context) : IRequestHandler<FailAfterWriteCommand, Result<Guid>>
{
    public const string Message = "Forced failure after write";

    public async Task<Result<Guid>> Handle(FailAfterWriteCommand request, CancellationToken cancellationToken)
    {
        var laboratorio = Laboratorio.Create(request.NombreCompania, null).Value!;
        _ = context.Laboratorios.Add(laboratorio);
        _ = await context.SaveChangesAsync(cancellationToken);

        throw new InvalidOperationException(Message);
    }
}
