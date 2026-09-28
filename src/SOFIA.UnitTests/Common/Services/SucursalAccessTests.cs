using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Services;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Common.Services;

public class SucursalAccessTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "hash").Value!;
    private readonly Cuenta _otraCuenta = Cuenta.Create(Guid.NewGuid(), "otro", "hash").Value!;
    private readonly Rol _rol = Rol.Create("Inventario", null).Value!;
    private readonly Rol _otroRol = Rol.Create("Otro", null).Value!;
    private readonly Sucursal _base = Sucursal.Create("Base", "Av. A 1", "LIC-A").Value!;
    private readonly Sucursal _asignada = Sucursal.Create("Asignada", "Av. B 2", "LIC-B").Value!;
    private readonly Sucursal _porRol = Sucursal.Create("Por rol", "Av. C 3", "LIC-C").Value!;
    private readonly Sucursal _otra = Sucursal.Create("Otra", "Av. D 4", "LIC-D").Value!;
    private readonly List<RolSucursal> _rolesSucursales = [];

    public SucursalAccessTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_base.Id.ToString());
        _ = _currentUserMock.Setup(u => u.CuentaId).Returns(_cuenta.Id.ToString());
        _otraCuenta.AddSucursal(_otra);
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(() => new List<Sucursal> { _base, _asignada, _porRol, _otra }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(() => new List<Cuenta> { _cuenta, _otraCuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.RolesSucursales).Returns(() => _rolesSucursales.BuildMockDbSet().Object);
    }

    private SucursalAccess CreateService() => new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task GetAllowedSucursalesAsync_ShouldReturnNull_WhenUserIsAdmin()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);
        var service = CreateService();

        var allowed = await service.GetAllowedSucursalesAsync(CancellationToken.None);

        _ = allowed.Should().BeNull();
        _ = (await service.CanAccessAsync(_otra.Id, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllowedSucursalesAsync_ShouldReturnOnlyBaseBranch_WhenThereAreNoAssignments()
    {
        var allowed = await CreateService().GetAllowedSucursalesAsync(CancellationToken.None);

        _ = allowed.Should().BeEquivalentTo([_base.Id]);
    }

    [Fact]
    public async Task GetAllowedSucursalesAsync_ShouldIncludeAccountAssignments()
    {
        _cuenta.AddSucursal(_asignada);

        var allowed = await CreateService().GetAllowedSucursalesAsync(CancellationToken.None);

        _ = allowed.Should().BeEquivalentTo([_base.Id, _asignada.Id]);
    }

    [Fact]
    public async Task GetAllowedSucursalesAsync_ShouldIncludeBranchesOfTheAccountRoles()
    {
        _cuenta.AddRol(_rol);
        _rolesSucursales.Add(RolSucursal.Create(_rol.Id, _porRol.Id));
        _rolesSucursales.Add(RolSucursal.Create(_otroRol.Id, _otra.Id));

        var allowed = await CreateService().GetAllowedSucursalesAsync(CancellationToken.None);

        _ = allowed.Should().BeEquivalentTo([_base.Id, _porRol.Id]);
    }

    [Fact]
    public async Task CanAccessAsync_ShouldUseTheUnionOfBaseAccountAndRoleBranches()
    {
        _cuenta.AddSucursal(_asignada);
        _cuenta.AddRol(_rol);
        _rolesSucursales.Add(RolSucursal.Create(_rol.Id, _porRol.Id));
        var service = CreateService();

        _ = (await service.CanAccessAsync(_base.Id, CancellationToken.None)).Should().BeTrue();
        _ = (await service.CanAccessAsync(_asignada.Id, CancellationToken.None)).Should().BeTrue();
        _ = (await service.CanAccessAsync(_porRol.Id, CancellationToken.None)).Should().BeTrue();
        _ = (await service.CanAccessAsync(_otra.Id, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task GetAllowedSucursalesAsync_ShouldIgnoreRoleBranchesOutsideTheTenant()
    {
        _cuenta.AddRol(_rol);
        _rolesSucursales.Add(RolSucursal.Create(_rol.Id, Guid.NewGuid()));

        var allowed = await CreateService().GetAllowedSucursalesAsync(CancellationToken.None);

        _ = allowed.Should().BeEquivalentTo([_base.Id]);
    }

    [Fact]
    public async Task GetAllowedSucursalesAsync_ShouldReturnEmpty_WhenUserIsNotAuthenticated()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);

        var allowed = await CreateService().GetAllowedSucursalesAsync(CancellationToken.None);

        _ = allowed.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllowedSucursalesAsync_ShouldQueryOncePerRequest()
    {
        var service = CreateService();

        _ = await service.GetAllowedSucursalesAsync(CancellationToken.None);
        _ = await service.CanAccessAsync(_base.Id, CancellationToken.None);

        _dbContextMock.Verify(c => c.Sucursales, Times.Once);
    }
}
