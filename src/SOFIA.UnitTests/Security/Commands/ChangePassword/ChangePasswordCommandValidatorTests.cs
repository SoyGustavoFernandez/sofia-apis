using FluentValidation.TestHelper;
using SOFIA.Application.Security.Commands.ChangePassword;

namespace SOFIA.UnitTests.Security.Commands.ChangePassword;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenNewPasswordIsStrongAndDifferent()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "ClaveActual1", "ClaveNueva1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenCuentaIdIsEmpty()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.Empty, "ClaveActual1", "ClaveNueva1"));
        _ = result.ShouldHaveValidationErrorFor(x => x.CuentaId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCurrentPasswordIsEmpty()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "", "ClaveNueva1"));
        _ = result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta1")]
    public void Validate_ShouldFail_WhenNewPasswordIsWeak(string newPassword)
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "ClaveActual1", newPassword));
        _ = result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNewPasswordEqualsCurrent()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "ClaveActual1", "ClaveActual1"));
        _ = result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
