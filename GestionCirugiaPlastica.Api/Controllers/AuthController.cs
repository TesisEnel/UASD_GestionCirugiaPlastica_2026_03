using System.Net.Mail;
using System.Text.RegularExpressions;
using GestionCirugiaPlastica.Api.Services;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
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
    JwtTokenService tokenService
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

        var token = tokenService.Create(user, user.Rol.Nombre);

        return Ok(new LoginResponse(
            token.AccessToken,
            token.ExpiresAtUtc,
            user.Nombre,
            user.Rol.Nombre));
        }
        catch (Exception ex)
        {
            // Diagnóstico temporal de desarrollo para mostrar la causa del 500.
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var fullName = request.NombreCompleto?.Trim();
        var username = request.NombreUsuario?.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        var cedula = string.IsNullOrWhiteSpace(request.Cedula) ? null : request.Cedula.Trim();
        var telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 250)
            return BadRequest(new { message = "Ingresa tu nombre completo (máximo 250 caracteres)." });
        if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 100 ||
            !Regex.IsMatch(username, @"^[\p{L}\p{N}._-]+$"))
            return BadRequest(new { message = "El nombre de usuario debe tener 3 a 100 caracteres y usar solo letras, números, punto, guion o guion bajo." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
            return BadRequest(new { message = "La contraseña debe tener al menos 12 caracteres." });
        if (email is not null && (email.Length > 250 || !MailAddress.TryCreate(email, out var mail) || mail.Address != email))
            return BadRequest(new { message = "El correo electrónico no es válido." });
        if (cedula?.Length > 20 || telefono?.Length > 250)
            return BadRequest(new { message = "La cédula o el teléfono superan la longitud permitida." });

        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var nombre = nameParts[0];
        var apellido = nameParts.Length > 1 ? nameParts[1] : "";
        if (nombre.Length > 250 || apellido.Length > 250)
            return BadRequest(new { message = "El nombre o apellido superan 250 caracteres." });

        await using var db = await contextFactory.CreateDbContextAsync();
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Nombre.ToLower() == "paciente");
        if (role is null)
            return Problem("No existe el rol Paciente. El administrador debe crear los roles antes de habilitar el registro.", statusCode: 500);

        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario != null && u.NombreUsuario.ToLower() == username.ToLower()))
            return Conflict(new { message = "Ese nombre de usuario ya está registrado." });
        if (email is not null && await db.Usuarios.AnyAsync(u => u.Email != null && u.Email.ToLower() == email))
            return Conflict(new { message = "Ese correo electrónico ya está registrado." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            Paciente paciente;
            if (cedula is not null)
            {
                var existingPatient = await db.Pacientes.FirstOrDefaultAsync(p => p.Cedula == cedula);
                if (existingPatient is not null)
                {
                    paciente = existingPatient;
                    // Para asociar una cuenta a una ficha existente se requiere verificar también
                    // el teléfono que ya consta en esa ficha.
                    if (string.IsNullOrWhiteSpace(telefono) ||
                        NormalizePhone(paciente.Telefono) != NormalizePhone(telefono))
                        return Conflict(new { message = "Ya existe una ficha con esa cédula. Para vincularla, ingresa el teléfono que consta en la clínica o solicita ayuda en recepción." });

                    if (await db.Usuarios.AnyAsync(u => u.PacienteId == paciente.PacienteId))
                        return Conflict(new { message = "Esa ficha ya tiene una cuenta vinculada. Contacta a recepción para recuperar el acceso." });
                }
                else
                {
                    paciente = NewPatient(nombre, apellido, cedula, telefono, email);
                    db.Pacientes.Add(paciente);
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                paciente = NewPatient(nombre, apellido, null, telefono, email);
                db.Pacientes.Add(paciente);
                await db.SaveChangesAsync();
            }

            var user = new Usuario
            {
                FechaRegistro = DateTime.Now,
                Nombre = nombre,
                Apellido = apellido,
                NombreUsuario = username,
                Email = email,
                PacienteId = paciente.PacienteId,
                RolId = role.RolId,
                Activo = true
            };
            user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user, request.Password);
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "La cuenta de paciente se creó correctamente. Ya puedes iniciar sesión.",
                user.UsuarioId
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "No se pudo registrar la cuenta porque alguno de los datos ya está registrado." });
        }
    }

    private static Paciente NewPatient(string nombre, string apellido, string? cedula, string? telefono, string? email) => new()
    {
        FechaRegistro = DateTime.Now,
        Nombre = nombre,
        Apellido = apellido,
        Cedula = cedula,
        Telefono = telefono,
        Email = email,
        TieneSeguro = false
    };

    private static string NormalizePhone(string? value) =>
        new((value ?? "").Where(char.IsDigit).ToArray());
}

public sealed record LoginRequest(string Identifier, string Password);
public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Name, string Role);
public sealed record RegisterRequest(string NombreCompleto, string NombreUsuario, string Password, string? Email, string? Cedula, string? Telefono);
