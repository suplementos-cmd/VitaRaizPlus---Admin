using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> GetUsers([FromQuery] string? searchTerm = null,
        [FromQuery] int? roleId = null,
        [FromQuery] bool? isActive = null)
    {
        var users = await _userRepository.GetUsersAsync(searchTerm, roleId, isActive);
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user is null)
            return NotFound(new { message = "Usuario no encontrado" });

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username, FullName y Password son requeridos" });

        var userId = await _userRepository.CreateUserAsync(
            request.Username,
            request.FullName,
            request.Email,
            request.Password,
            request.RoleId,
            request.ZoneId,
            request.IsActive,
            GetCurrentUserId());

        return CreatedAtAction(nameof(GetUserById), new { id = userId }, new { userId });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (id != request.UserId)
            return BadRequest(new { message = "El ID del usuario no coincide" });

        var updated = await _userRepository.UpdateUserAsync(
            request.UserId,
            request.FullName,
            request.Email,
            request.RoleId,
            request.ZoneId,
            request.IsActive,
            GetCurrentUserId());

        if (!updated)
            return NotFound(new { message = "Usuario no encontrado" });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var deleted = await _userRepository.DeleteUserAsync(id, GetCurrentUserId());
        if (!deleted)
            return NotFound(new { message = "Usuario no encontrado" });

        return NoContent();
    }

    [HttpPost("{id:int}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("AdminFull") || User.IsInRole("Admin");

        if (!isAdmin && currentUserId != id)
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "CurrentPassword y NewPassword son requeridos" });

        var changed = await _userRepository.ChangePasswordAsync(id, request.CurrentPassword, request.NewPassword);
        if (!changed)
            return BadRequest(new { message = "La contraseña actual no es válida" });

        return Ok(new { success = true });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return int.TryParse(claim, out var userId) ? userId : null;
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? ZoneId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateUserRequest
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int RoleId { get; set; }
        public int? ZoneId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
