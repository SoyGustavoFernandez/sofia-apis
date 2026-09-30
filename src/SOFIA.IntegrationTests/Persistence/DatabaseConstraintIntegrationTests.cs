using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOFIA.API.Infrastructure;
using SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;
using SOFIA.Application.Pacientes.Commands.CreatePaciente;
using SOFIA.Application.Pacientes.Commands.DeletePaciente;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Persistence;

// Real SQL Server errors from the test container: soft-delete aware uniques and the GlobalExceptionHandler mapping of genuine SqlException numbers
public class DatabaseConstraintIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private readonly CapturingLogger _logger = new();

    [Fact]
    public async Task CreatePaciente_ShouldSucceed_WhenTheSameDocumentOnlyBelongsToADeletedPaciente()
    {
        // Arrange
        var dni = $"{Random.Shared.Next(10_000_000, 99_999_999)}";
        var primero = await Sender.Send(new CreatePacienteCommand(dni, "Juan Perez", new DateOnly(1990, 1, 1), null));
        _ = primero.IsSuccess.Should().BeTrue();
        _ = (await Sender.Send(new DeletePacienteCommand(primero.Value))).IsSuccess.Should().BeTrue();

        // Act
        var segundo = await Sender.Send(new CreatePacienteCommand(dni, "Juan Perez", new DateOnly(1990, 1, 1), null));

        // Assert
        _ = segundo.IsSuccess.Should().BeTrue(because: "the unique index only counts live rows");
        var vivos = await DbContext.Pacientes.CountAsync(p => p.DocIdentidadGub == dni);
        _ = vivos.Should().Be(1);
    }

    [Fact]
    public async Task CreatePaciente_ShouldReturnConflict_WhenTheDocumentBelongsToALivePaciente()
    {
        var dni = $"{Random.Shared.Next(10_000_000, 99_999_999)}";
        _ = (await Sender.Send(new CreatePacienteCommand(dni, "Juan Perez", new DateOnly(1990, 1, 1), null))).IsSuccess.Should().BeTrue();

        var duplicado = await Sender.Send(new CreatePacienteCommand(dni, "Otro Nombre", new DateOnly(1985, 5, 5), null));

        _ = duplicado.IsFailure.Should().BeTrue();
        _ = duplicado.Error.Code.Should().Be("Paciente.DocIdentidadGub.Duplicado");
        _ = duplicado.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task CreateLaboratorio_ShouldSucceed_WhenAnotherLaboratorioAlsoHasNoCodigo()
    {
        var primero = await Sender.Send(new CreateLaboratorioCommand($"Lab{Guid.NewGuid():N}"[..20], null));
        var segundo = await Sender.Send(new CreateLaboratorioCommand($"Lab{Guid.NewGuid():N}"[..20], ""));

        _ = primero.IsSuccess.Should().BeTrue();
        _ = segundo.IsSuccess.Should().BeTrue(because: "labs without code never collide on the filtered unique index");
        var guardado = await DbContext.Laboratorios.AsNoTracking().SingleAsync(l => l.Id == segundo.Value);
        _ = guardado.CodigoIdentificador.Should().BeNull();
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectASecondLiveRowWithTheSameKey_WhenTheApplicationCheckIsBypassed()
    {
        var dni = $"{Random.Shared.Next(10_000_000, 99_999_999)}";
        _ = DbContext.Pacientes.Add(PacienteCliente.Create(dni, "Juan Perez", new DateOnly(1990, 1, 1), null).Value!);
        _ = await DbContext.SaveChangesAsync();

        _ = DbContext.Pacientes.Add(PacienteCliente.Create(dni, "Otro", new DateOnly(1990, 1, 1), null).Value!);
        var act = () => DbContext.SaveChangesAsync();

        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        _ = thrown.Which.InnerException!.Message.Should().Contain("UX_Pacientes_Clientes_Tenant_Doc_Identidad_Gub");
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectASecondLiveEmpleadoWithTheSameEmail_WhileEmpleadosWithoutEmailNeverCollide()
    {
        var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = DbContext.Sucursales.Add(sucursal);
        var email = $"qa.{Guid.NewGuid():N}@sofia.test";
        _ = DbContext.Empleados.Add(Empleado.Create(sucursal.Id, "Ana", "Perez", "Gomez", email: email).Value!);
        _ = DbContext.Empleados.Add(Empleado.Create(sucursal.Id, "Sin", "Correo", "Uno").Value!);
        _ = DbContext.Empleados.Add(Empleado.Create(sucursal.Id, "Sin", "Correo", "Dos").Value!);
        _ = await DbContext.SaveChangesAsync();

        _ = DbContext.Empleados.Add(Empleado.Create(sucursal.Id, "Luis", "Rios", "Paz", email: email).Value!);
        var act = () => DbContext.SaveChangesAsync();

        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        _ = thrown.Which.InnerException!.Message.Should().Contain("UX_Empleados_Tenant_Email");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnConflictWithoutLoggingTheKey_WhenAUniqueIndexIsViolated()
    {
        var dni = $"{Random.Shared.Next(10_000_000, 99_999_999)}";
        _ = DbContext.Pacientes.Add(PacienteCliente.Create(dni, "Juan Perez", new DateOnly(1990, 1, 1), null).Value!);
        _ = await DbContext.SaveChangesAsync();
        _ = DbContext.Pacientes.Add(PacienteCliente.Create(dni, "Otro", new DateOnly(1990, 1, 1), null).Value!);
        var exception = await CaptureAsync(() => DbContext.SaveChangesAsync());
        _ = ((SqlException)exception.InnerException!).Number.Should().Be(2601);

        var (context, body) = await HandleAsync(exception);

        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        _ = body.RootElement.GetProperty("code").GetString().Should().Be(GlobalExceptionHandler.DuplicadoCode);
        _ = _logger.Messages.Should().ContainSingle().Which.Should().Contain("UX_Pacientes_Clientes_Tenant_Doc_Identidad_Gub").And.NotContain(dni);
        _ = body.RootElement.GetRawText().Should().NotContain(dni);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnConflict_WhenAUniqueConstraintIsViolated()
    {
        var sqlException = await CaptureSqlAsync("CREATE TABLE #t (C INT CONSTRAINT UQ_T_C UNIQUE); INSERT INTO #t VALUES (7); INSERT INTO #t VALUES (7);");
        _ = sqlException.Number.Should().Be(2627);

        var (context, body) = await HandleAsync(new DbUpdateException("save failed", sqlException));

        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        _ = body.RootElement.GetProperty("code").GetString().Should().Be(GlobalExceptionHandler.DuplicadoCode);
        _ = _logger.Messages.Should().ContainSingle().Which.Should().Contain("UQ_T_C");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnConflict_WhenAForeignKeyIsViolated()
    {
        var unidad = UnidadMedida.Create($"U{Guid.NewGuid():N}"[..10], "Unidad").Value!;
        _ = DbContext.UnidadesMedida.Add(unidad);
        _ = await DbContext.SaveChangesAsync();
        _ = DbContext.Medicamentos.Add(Medicamento.Create($"MED-{Guid.NewGuid():N}"[..20], "Huerfano", Guid.NewGuid(), unidad.Id, CondicionVenta.VentaLibreOTC).Value!);
        var exception = await CaptureAsync(() => DbContext.SaveChangesAsync());
        _ = ((SqlException)exception.InnerException!).Number.Should().Be(547);

        var (context, body) = await HandleAsync(exception);

        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        _ = body.RootElement.GetProperty("code").GetString().Should().Be(GlobalExceptionHandler.ReferenciaInvalidaCode);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnBadRequest_WhenACheckConstraintIsViolated()
    {
        var sqlException = await CaptureSqlAsync("CREATE TABLE #t (C INT CONSTRAINT CHK_T_C CHECK (C >= 0)); INSERT INTO #t VALUES (-1);");
        _ = sqlException.Number.Should().Be(547);

        var (context, body) = await HandleAsync(new DbUpdateException("save failed", sqlException));

        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _ = body.RootElement.GetProperty("code").GetString().Should().Be(GlobalExceptionHandler.ValorInvalidoCode);
        _ = _logger.Messages.Should().ContainSingle().Which.Should().Contain("CHK_T_C");
    }

    private static async Task<DbUpdateException> CaptureAsync(Func<Task> save)
    {
        try
        {
            await save();
        }
        catch (DbUpdateException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("The save was expected to violate a constraint.");
    }

    private async Task<SqlException> CaptureSqlAsync(string sql)
    {
        try
        {
            _ = await DbContext.Database.ExecuteSqlRawAsync(sql);
        }
        catch (SqlException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("The statement was expected to violate a constraint.");
    }

    private async Task<(HttpContext Context, JsonDocument Body)> HandleAsync(Exception exception)
    {
        var handler = new GlobalExceptionHandler(_logger);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/pacientes";
        context.Response.Body = new MemoryStream();

        _ = (await handler.TryHandleAsync(context, exception, CancellationToken.None)).Should().BeTrue();

        context.Response.Body.Position = 0;
        return (context, await JsonDocument.ParseAsync(context.Response.Body));
    }

    private sealed class CapturingLogger : ILogger<GlobalExceptionHandler>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception) + exception);
    }
}
