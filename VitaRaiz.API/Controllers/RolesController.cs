using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;

    public RolesController(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    [HttpGet]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleRepository.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("permissions")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _roleRepository.GetPermissionsAsync();
        return Ok(permissions);
    }

    [HttpGet("{id:int}/permissions")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> GetRolePermissions(int id)
    {
        var permissions = await _roleRepository.GetRolePermissionsAsync(id);
        return Ok(permissions);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        if (id != request.RoleId)
            return BadRequest(new { message = "El ID del rol no coincide" });

        await _roleRepository.UpdateRoleAsync(request.RoleId, request.RoleName, request.Description,
            request.DefaultThemeColor, GetCurrentUserId());

        return NoContent();
    }

    [HttpPut("{id:int}/permissions")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> SetRolePermissions(int id, [FromBody] SetRolePermissionsRequest request)
    {
        await _roleRepository.SetRolePermissionsAsync(id, request.Permissions ?? new List<string>(), GetCurrentUserId());
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "AdminFull")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
            return BadRequest(new { message = "El nombre del rol es requerido" });
        try
        {
            var roleId = await _roleRepository.CreateRoleAsync(
                request.RoleName, request.Description, request.DefaultThemeColor);
            return CreatedAtAction(nameof(GetRoles), new { }, new { roleId, message = "Rol creado" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return int.TryParse(claim, out var userId) ? userId : null;
    }

    public class UpdateRoleRequest
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DefaultThemeColor { get; set; }
    }

    public class SetRolePermissionsRequest
    {
        public List<string>? Permissions { get; set; }
    }

    public class CreateRoleRequest
    {
        public string  RoleName           { get; set; } = string.Empty;
        public string? Description        { get; set; }
        public string? DefaultThemeColor  { get; set; }
    }
}
