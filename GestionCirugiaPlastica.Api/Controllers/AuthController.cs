using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionCirugiaPlastica.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GestionCirugiaPlastica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory,
    IConfiguration configuration
) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
        var identifier = request.Identifier?.Trim();
        var normalizedIdentifier = identifier?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });

        await using var db = await contextFactory.CreateDbContextAsync();
        var users = await db.Usuarios
            .Include(u => u.Rol)
            .Where(u =>
                ((u.NombreUsuario != null && u.NombreUsuario.Trim().ToLower() == normalizedIdentifier) ||
                 (u.Email != null && u.Email.Trim().ToLower() == normalizedIdentifier) ||
                 (u.PacienteId != null && db.Pacientes.Any(p => p.PacienteId == u.PacienteId &&
                     (p.Cedula != null && p.Cedula.Trim() == identifier ||
                      p.Telefono != null && p.Telefono.Trim() == identifier)))))
            .Take(2)
            .ToListAsync();

        if (users.Count == 0)
            return Unauthorized(new { message = "Usuario no encontrado. Verifica el nombre de usuario o correo." });
        if (users.Count > 1)
            return Unauthorized(new { message = "El identificador coincide con más de una cuenta. Contacta al administrador." });

        var user = users[0];
        if (!user.Activo)
            return Unauthorized(new { message = "La cuenta está inactiva." });
        if (user.Rol is null)
            return Unauthorized(new { message = "La cuenta no tiene un rol asignado." });

        var check = new PasswordHasher<GestionCirugiaPlastica.Infrastructure.Models.Usuario>()
            .VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (check == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Contraseña inválida para esta cuenta." });

        var keyText = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(keyText) || Encoding.UTF8.GetByteCount(keyText) < 32)
            return Problem("JWT signing key is not configured. Set Jwt__Key to a secret of at least 32 bytes.", statusCode: 500);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UsuarioId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
            new(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}".Trim()),
            new(ClaimTypes.Role, user.Rol.Nombre)
        };
        if (user.PacienteId is int pacienteId)
            claims.Add(new Claim("paciente_id", pacienteId.ToString()));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "GestionCirugiaPlastica.Api",
            audience: configuration["Jwt:Audience"] ?? "GestionCirugiaPlastica.Blazor",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return Ok(new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            token.ValidTo,
            user.Nombre,
            user.Rol.Nombre));
        }
        catch (Exception ex)
        {
            // Diagnóstico temporal de desarrollo para mostrar la causa del 500.
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}

public sealed record LoginRequest(string Identifier, string Password);
public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Name, string Role);
