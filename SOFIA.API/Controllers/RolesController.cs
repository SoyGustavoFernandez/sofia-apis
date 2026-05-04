using MediatR;
using Microsoft.AspNetCore.Authorization;
using SOFIA.Application.Security.Commands.Roles.AssignPermission;
using SOFIA.Application.Security.Commands.Roles.AssignRol;
using SOFIA.Application.Security.Commands.Roles.CreateRol;
using SOFIA.Application.Security.Commands.Roles.DeleteRol;
using SOFIA.Application.Security.Commands.Roles.RemoveRol;
using SOFIA.Application.Security.Commands.Roles.RevokePermission;
using SOFIA.Application.Security.Commands.Roles.UpdateRol;
using SOFIA.Application.Security.Queries.Roles.GetPermissions;
using SOFIA.Application.Security.Queries.Roles.GetRoles;
using SOFIA.Application.Security.Queries.Roles.GetRolById;

namespace SOFIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var result = await sender.Send(new GetRolesQuery());
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRolById(Guid id)
    {
        var result = await sender.Send(new GetRolByIdQuery(id));
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRol([FromBody] CreateRolCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetRoles), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRol(Guid id, [FromBody] UpdateRolRequest request)
    {
        var result = await sender.Send(new UpdateRolCommand(id, request.Descripcion, request.NivelJerarquia));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRol(Guid id)
    {
        var result = await sender.Send(new DeleteRolCommand(id));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost("assign-to-user")]
    public async Task<IActionResult> AssignRol([FromBody] AssignRolToUserCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost("remove-from-user")]
    public async Task<IActionResult> RemoveRol([FromBody] RemoveRolFromUserCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid id)
    {
        var result = await sender.Send(new GetPermissionsByRolQuery(id));
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpPost("permissions")]
    public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionToRolCommand command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess
            ? Ok(new { Id = result.Value })
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }

    [HttpDelete("permissions/{permisoId:guid}")]
    public async Task<IActionResult> RevokePermission(Guid permisoId)
    {
        var result = await sender.Send(new RevokePermissionFromRolCommand(permisoId));
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: result.StatusCode);
    }
}

public record UpdateRolRequest(string? Descripcion, int NivelJerarquia);
