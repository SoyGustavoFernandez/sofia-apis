using FluentValidation.TestHelper;
using SOFIA.Application.Empresas.Commands.ActivarSuscripcionEmpresa;
using SOFIA.Application.Empresas.Commands.CancelarEmpresa;
using SOFIA.Application.Empresas.Commands.ExtenderPruebaEmpresa;
using SOFIA.Application.Empresas.Commands.SuspenderEmpresa;

namespace SOFIA.UnitTests.Empresas.Commands.Suscripcion;

public class SuscripcionEmpresaCommandValidatorTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();

    [Fact]
    public void Activar_ShouldPass_WithIdAndFutureDate()
    {
        var command = new ActivarSuscripcionEmpresaCommand { Id = EmpresaId, FechaVencimiento = DateTimeOffset.UtcNow.AddDays(30) };
        new ActivarSuscripcionEmpresaCommandValidator().TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Activar_ShouldFail_WhenIdIsEmptyOrDateIsNotInTheFuture()
    {
        var command = new ActivarSuscripcionEmpresaCommand { Id = Guid.Empty, FechaVencimiento = DateTimeOffset.UtcNow.AddMinutes(-1) };
        var result = new ActivarSuscripcionEmpresaCommandValidator().TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(x => x.Id);
        _ = result.ShouldHaveValidationErrorFor(x => x.FechaVencimiento);
    }

    [Fact]
    public void ExtenderPrueba_ShouldPass_WithIdAndFutureDate()
    {
        var command = new ExtenderPruebaEmpresaCommand { Id = EmpresaId, FechaVencimiento = DateTimeOffset.UtcNow.AddDays(15) };
        new ExtenderPruebaEmpresaCommandValidator().TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ExtenderPrueba_ShouldFail_WhenIdIsEmptyOrDateIsNotInTheFuture()
    {
        var command = new ExtenderPruebaEmpresaCommand { Id = Guid.Empty, FechaVencimiento = default };
        var result = new ExtenderPruebaEmpresaCommandValidator().TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(x => x.Id);
        _ = result.ShouldHaveValidationErrorFor(x => x.FechaVencimiento);
    }

    [Fact]
    public void SuspenderAndCancelar_ShouldFail_WhenIdIsEmpty()
    {
        _ = new SuspenderEmpresaCommandValidator().TestValidate(new SuspenderEmpresaCommand(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.Id);
        _ = new CancelarEmpresaCommandValidator().TestValidate(new CancelarEmpresaCommand(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void SuspenderAndCancelar_ShouldPass_WithId()
    {
        new SuspenderEmpresaCommandValidator().TestValidate(new SuspenderEmpresaCommand(EmpresaId)).ShouldNotHaveAnyValidationErrors();
        new CancelarEmpresaCommandValidator().TestValidate(new CancelarEmpresaCommand(EmpresaId)).ShouldNotHaveAnyValidationErrors();
    }
}
