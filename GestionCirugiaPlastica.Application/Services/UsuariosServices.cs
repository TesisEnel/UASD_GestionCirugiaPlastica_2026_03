using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net.Mail;

namespace GestionCirugiaPlastica.Application.Services;

public class UsuariosServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Usuario, int>
{
    private readonly PasswordHasher<Usuario> passwordHasher = new();

    private const string RolAdministrador = "Administrador";

    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error DatosRequeridos =
        new("Usuario.DatosRequeridos", "Nombre, apellido y correo son obligatorios.");

    private static readonly Error TextoMuyLargo =
        new("Usuario.TextoMuyLargo", "Nombre, apellido y correo no pueden pasar de 250 caracteres.");

    private static readonly Error EmailInvalido =
        new("Usuario.EmailInvalido", "El correo electrónico no es válido.");

    private static readonly Error EmailDuplicado =
        new("Usuario.EmailDuplicado", "Ya existe un usuario con ese correo.");

    private static readonly Error RolNoExiste =
        new("Usuario.RolNoExiste", "El rol indicado no existe.");

    private static readonly Error PasswordInvalida =
        new("Usuario.PasswordInvalida", "La contraseña debe tener al menos 12 caracteres.");

    private static readonly Error UsuarioNoExiste =
        new("Usuario.NoExiste", "El usuario indicado no existe.");

    private static readonly Error RolSoloConCambiarRol =
        new("Usuario.RolSoloConCambiarRol", "El rol solo se cambia con la operación Cambiar rol.");

    private static readonly Error MismoRol =
        new("Usuario.MismoRol", "El usuario ya tiene ese rol.");

    private static readonly Error UltimoAdministrador =
        new("Usuario.UltimoAdministrador", "No se puede quitar o desactivar al último administrador activo.");

    private static readonly Error CredencialesInvalidas =
        new("Usuario.CredencialesInvalidas", "La contraseña actual no es correcta o el usuario está inactivo.");

    private static readonly Error PasswordNuevaInvalida =
        new("Usuario.PasswordNuevaInvalida", "La nueva contraseña debe tener 12 o más caracteres y ser distinta de la actual.");

    private static readonly Error YaEstaActivo =
        new("Usuario.YaActivo", "El usuario ya está activo.");

    private static readonly Error YaEstaInactivo =
        new("Usuario.YaInactivo", "El usuario ya está inactivo.");

    private static readonly Error NoSeEliminan =
        new("Usuario.NoSeElimina", "Los usuarios no se eliminan para conservar el historial de la clínica.");

    private static readonly Error ErrorAlGuardar =
        new("Usuario.ErrorAlGuardar", "No se pudo guardar. Verifique que el correo no esté duplicado.");

    private static bool EsAdministrador(string nombreRol) =>
        nombreRol.Equals(RolAdministrador, StringComparison.OrdinalIgnoreCase);

    private static bool EmailValido(string email) =>
        MailAddress.TryCreate(email, out var direccion) && direccion.Address == email;

    // 1. Registrar o actualizar usuario
    public async Task<Result> Guardar(Usuario usuario)
    {
        if (usuario == null ||
            string.IsNullOrWhiteSpace(usuario.Nombre) ||
            string.IsNullOrWhiteSpace(usuario.Apellido) ||
            (string.IsNullOrWhiteSpace(usuario.Email) && string.IsNullOrWhiteSpace(usuario.NombreUsuario) && usuario.PacienteId is null))
        {
            return Result.Failure(DatosRequeridos);
        }

        if (usuario.Nombre.Trim().Length > 250 ||
            usuario.Apellido.Trim().Length > 250 ||
            usuario.Email?.Trim().Length is > 250 ||
            usuario.NombreUsuario?.Trim().Length is > 100)
        {
            return Result.Failure(TextoMuyLargo);
        }

        string? email = string.IsNullOrWhiteSpace(usuario.Email) ? null : usuario.Email.Trim().ToLowerInvariant();
        string? nombreUsuario = string.IsNullOrWhiteSpace(usuario.NombreUsuario) ? null : usuario.NombreUsuario.Trim();

        if (email is not null && !EmailValido(email))
            return Result.Failure(EmailInvalido);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool emailExiste = email is not null && await contexto.Usuarios
            .AnyAsync(u => u.Email != null && u.Email.ToLower() == email && u.UsuarioId != usuario.UsuarioId);

        if (emailExiste)
            return Result.Failure(EmailDuplicado);

        bool nombreUsuarioExiste = nombreUsuario is not null && await contexto.Usuarios
            .AnyAsync(u => u.NombreUsuario != null && u.NombreUsuario.ToLower() == nombreUsuario.ToLower() && u.UsuarioId != usuario.UsuarioId);
        if (nombreUsuarioExiste)
            return Result.Failure(new Error("Usuario.NombreUsuarioDuplicado", "Ya existe un usuario con ese nombre de usuario."));

        bool rolExiste = await contexto.Roles
            .AnyAsync(r => r.RolId == usuario.RolId);

        if (!rolExiste)
            return Result.Failure(RolNoExiste);

        try
        {
            if (usuario.UsuarioId == 0)
            {
                // Al registrar, PasswordHash debe contener
                // la contraseña original que se desea proteger.
                if (string.IsNullOrWhiteSpace(usuario.PasswordHash) ||
                    usuario.PasswordHash.Length < 12)
                {
                    return Result.Failure(PasswordInvalida);
                }

                var nuevoUsuario = new Usuario
                {
                    FechaRegistro = DateTime.Now,
                    Nombre = usuario.Nombre.Trim(),
                    Apellido = usuario.Apellido.Trim(),
                    Email = email,
                    NombreUsuario = nombreUsuario,
                    PacienteId = usuario.PacienteId,
                    RolId = usuario.RolId,
                    Activo = true
                };

                nuevoUsuario.PasswordHash =
                    passwordHasher.HashPassword(nuevoUsuario, usuario.PasswordHash);

                contexto.Usuarios.Add(nuevoUsuario);
            }
            else
            {
                var existente = await contexto.Usuarios
                    .FirstOrDefaultAsync(u => u.UsuarioId == usuario.UsuarioId);

                if (existente == null)
                    return Result.Failure(UsuarioNoExiste);

                // El rol NO se cambia por aquí: CambiarRol protege al último
                // administrador y Guardar se saltaba esa protección.
                if (existente.RolId != usuario.RolId)
                    return Result.Failure(RolSoloConCambiarRol);

                existente.Nombre = usuario.Nombre.Trim();
                existente.Apellido = usuario.Apellido.Trim();
                existente.Email = email;
                existente.NombreUsuario = nombreUsuario;
                existente.PacienteId = usuario.PacienteId;

                // La contraseña se modifica únicamente mediante CambiarPassword.
                // El estado Activo, mediante Activar o Desactivar.
            }

            await contexto.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // 2. Buscar usuario por ID
    public async Task<Result<Usuario?>> Buscar(int usuarioId)
    {
        if (usuarioId <= 0)
            return Result<Usuario?>.Success((Usuario?)null);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuario = await contexto.Usuarios
            .AsNoTracking()
            .Include(u => u.Rol)
            .Where(u => u.UsuarioId == usuarioId)
            .Select(u => new Usuario
            {
                UsuarioId = u.UsuarioId,
                FechaRegistro = u.FechaRegistro,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                RolId = u.RolId,
                Rol = u.Rol,
                Activo = u.Activo,
                PasswordHash = string.Empty
            })
            .FirstOrDefaultAsync();

        return Result<Usuario?>.Success(usuario);
    }

    // 3. Obtener usuarios según un criterio
    public async Task<Result<List<Usuario>>> GetList(
        Expression<Func<Usuario, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuarios = await contexto.Usuarios
            .AsNoTracking()
            .Where(criterio)
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .Select(u => new Usuario
            {
                UsuarioId = u.UsuarioId,
                FechaRegistro = u.FechaRegistro,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                RolId = u.RolId,
                Rol = u.Rol,
                Activo = u.Activo,
                PasswordHash = string.Empty
            })
            .ToListAsync();

        return Result<List<Usuario>>.Success(usuarios);
    }

    // 4. Buscar usuarios por nombre o apellido
    public async Task<Result<List<Usuario>>> BuscarPorNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<List<Usuario>>.Success(new List<Usuario>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        nombre = nombre.Trim();

        var usuarios = await contexto.Usuarios
            .AsNoTracking()
            .Where(u =>
                u.Nombre.Contains(nombre) ||
                u.Apellido.Contains(nombre) ||
                (u.Nombre + " " + u.Apellido).Contains(nombre))
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .Select(u => new Usuario
            {
                UsuarioId = u.UsuarioId,
                FechaRegistro = u.FechaRegistro,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                RolId = u.RolId,
                Rol = u.Rol,
                Activo = u.Activo,
                PasswordHash = string.Empty
            })
            .ToListAsync();

        return Result<List<Usuario>>.Success(usuarios);
    }

    // 5. Buscar usuario por email
    public async Task<Result<Usuario?>> BuscarPorEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Usuario?>.Success((Usuario?)null);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        email = email.Trim().ToLowerInvariant();

        var usuario = await contexto.Usuarios
            .AsNoTracking()
            .Include(u => u.Rol)
            .Where(u => u.Email != null && u.Email.ToLower() == email)
            .Select(u => new Usuario
            {
                UsuarioId = u.UsuarioId,
                FechaRegistro = u.FechaRegistro,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                RolId = u.RolId,
                Rol = u.Rol,
                Activo = u.Activo,
                PasswordHash = string.Empty
            })
            .FirstOrDefaultAsync();

        return Result<Usuario?>.Success(usuario);
    }

    // 6. Buscar usuarios por rol
    public async Task<Result<List<Usuario>>> BuscarPorRol(int rolId)
    {
        if (rolId <= 0)
            return Result<List<Usuario>>.Success(new List<Usuario>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuarios = await contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.RolId == rolId)
            .OrderBy(u => u.Nombre)
            .Select(u => new Usuario
            {
                UsuarioId = u.UsuarioId,
                FechaRegistro = u.FechaRegistro,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                RolId = u.RolId,
                Rol = u.Rol,
                Activo = u.Activo,
                PasswordHash = string.Empty
            })
            .ToListAsync();

        return Result<List<Usuario>>.Success(usuarios);
    }

    // 7. Buscar usuarios activos
    public async Task<Result<List<Usuario>>> BuscarActivos()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuarios = await contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.Nombre)
            .Select(u => new Usuario
            {
                UsuarioId = u.UsuarioId,
                FechaRegistro = u.FechaRegistro,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                RolId = u.RolId,
                Rol = u.Rol,
                Activo = u.Activo,
                PasswordHash = string.Empty
            })
            .ToListAsync();

        return Result<List<Usuario>>.Success(usuarios);
    }

    // 8. Cambiar rol de un usuario
    public async Task<Result> CambiarRol(int usuarioId, int nuevoRolId)
    {
        if (usuarioId <= 0)
            return Result.Failure(UsuarioNoExiste);

        if (nuevoRolId <= 0)
            return Result.Failure(RolNoExiste);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuario = await contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

        if (usuario == null)
            return Result.Failure(UsuarioNoExiste);

        var nuevoRol = await contexto.Roles
            .FirstOrDefaultAsync(r => r.RolId == nuevoRolId);

        if (nuevoRol == null)
            return Result.Failure(RolNoExiste);

        if (usuario.RolId == nuevoRolId)
            return Result.Failure(MismoRol);

        // Proteger el último administrador activo
        if (usuario.Activo &&
            EsAdministrador(usuario.Rol.Nombre) &&
            !EsAdministrador(nuevoRol.Nombre))
        {
            bool otroAdministrador = await contexto.Usuarios
                .AnyAsync(u =>
                    u.UsuarioId != usuarioId &&
                    u.Activo &&
                    u.Rol.Nombre.ToLower() == RolAdministrador.ToLower());

            if (!otroAdministrador)
                return Result.Failure(UltimoAdministrador);
        }

        usuario.RolId = nuevoRolId;
        await contexto.SaveChangesAsync();
        return Result.Success();
    }

    // 9. Cambiar contraseña
    public async Task<Result> CambiarPassword(
        int usuarioId,
        string passwordActual,
        string passwordNueva)
    {
        if (string.IsNullOrWhiteSpace(passwordActual) ||
            string.IsNullOrWhiteSpace(passwordNueva) ||
            passwordNueva.Length < 12 ||
            passwordActual == passwordNueva)
        {
            return Result.Failure(PasswordNuevaInvalida);
        }

        if (usuarioId <= 0)
            return Result.Failure(UsuarioNoExiste);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuario = await contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

        if (usuario == null)
            return Result.Failure(UsuarioNoExiste);

        if (!usuario.Activo)
            return Result.Failure(CredencialesInvalidas);

        var verificacion = passwordHasher.VerifyHashedPassword(
            usuario, usuario.PasswordHash, passwordActual);

        if (verificacion == PasswordVerificationResult.Failed)
            return Result.Failure(CredencialesInvalidas);

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, passwordNueva);

        await contexto.SaveChangesAsync();
        return Result.Success();
    }

    // 10. Desactivar usuario
    public async Task<Result> Desactivar(int usuarioId)
    {
        if (usuarioId <= 0)
            return Result.Failure(UsuarioNoExiste);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuario = await contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

        if (usuario == null)
            return Result.Failure(UsuarioNoExiste);

        if (!usuario.Activo)
            return Result.Failure(YaEstaInactivo);

        if (EsAdministrador(usuario.Rol.Nombre))
        {
            bool otroAdministrador = await contexto.Usuarios
                .AnyAsync(u =>
                    u.UsuarioId != usuarioId &&
                    u.Activo &&
                    u.Rol.Nombre.ToLower() == RolAdministrador.ToLower());

            if (!otroAdministrador)
                return Result.Failure(UltimoAdministrador);
        }

        usuario.Activo = false;
        await contexto.SaveChangesAsync();
        return Result.Success();
    }

    // 11. Activar usuario
    public async Task<Result> Activar(int usuarioId)
    {
        if (usuarioId <= 0)
            return Result.Failure(UsuarioNoExiste);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuario = await contexto.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

        if (usuario == null)
            return Result.Failure(UsuarioNoExiste);

        if (usuario.Activo)
            return Result.Failure(YaEstaActivo);

        usuario.Activo = true;
        await contexto.SaveChangesAsync();
        return Result.Success();
    }

    // 12. Verificar si un email está registrado
    public async Task<Result<bool>> VerificarEmail(
        string email,
        int usuarioIdExcluir = 0)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<bool>.Success(false);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        email = email.Trim().ToLowerInvariant();

        bool existe = await contexto.Usuarios
            .AnyAsync(u =>
                u.Email != null && u.Email.ToLower() == email &&
                u.UsuarioId != usuarioIdExcluir);

        return Result<bool>.Success(existe);
    }

    // 13. No permitir eliminar usuarios
    // Se conservan los registros históricos de la clínica.
    public Task<Result> Eliminar(int usuarioId)
    {
        return Task.FromResult(Result.Failure(NoSeEliminan));
    }
}
