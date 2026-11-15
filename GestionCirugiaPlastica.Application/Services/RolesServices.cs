using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class RolesServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Role, int>
{
    private const string RolAdministrador = "Administrador";

    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error RolRequerido =
        new("Rol.Requerido", "Debe enviar los datos del rol.");

    private static readonly Error NombreRequerido =
        new("Rol.NombreRequerido", "El nombre del rol es obligatorio.");

    private static readonly Error NombreMuyLargo =
        new("Rol.NombreMuyLargo", "El nombre del rol no puede pasar de 250 caracteres.");

    private static readonly Error DescripcionMuyLarga =
        new("Rol.DescripcionMuyLarga", "La descripción del rol no puede pasar de 250 caracteres.");

    private static readonly Error NombreDuplicado =
        new("Rol.NombreDuplicado", "Ya existe un rol con ese nombre.");

    private static readonly Error RolNoExiste =
        new("Rol.NoExiste", "El rol indicado no existe.");

    private static readonly Error AdministradorNoRenombrable =
        new("Rol.AdministradorNoRenombrable", "El rol Administrador no se puede renombrar.");

    private static readonly Error AdministradorNoEliminable =
        new("Rol.AdministradorNoEliminable", "El rol Administrador no se puede eliminar.");

    private static readonly Error RolConUsuarios =
        new("Rol.TieneUsuarios", "No se puede eliminar un rol que tiene usuarios asignados.");

    private static readonly Error ErrorAlGuardar =
        new("Rol.ErrorAlGuardar", "No se pudo guardar el rol.");

    private static readonly Error ErrorAlEliminar =
        new("Rol.ErrorAlEliminar", "No se pudo eliminar el rol.");

    // ── Guardar ────────────────────────────────────────────────────────────
    // Guardar o actualizar un rol
    public async Task<Result> Guardar(Role rol)
    {
        if (rol == null)
            return Result.Failure(RolRequerido);

        if (string.IsNullOrWhiteSpace(rol.Nombre))
            return Result.Failure(NombreRequerido);

        if (rol.Nombre.Trim().Length > 250)
            return Result.Failure(NombreMuyLargo);

        if (rol.Descripcion?.Trim().Length > 250)
            return Result.Failure(DescripcionMuyLarga);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        string nombre = rol.Nombre.Trim();

        string? descripcion = string.IsNullOrWhiteSpace(rol.Descripcion)
            ? null
            : rol.Descripcion.Trim();

        // El nombre es único (sin distinguir mayúsculas), excluyendo el propio registro.
        bool nombreExiste = await contexto.Roles
            .AnyAsync(r =>
                r.Nombre.ToLower() == nombre.ToLower() &&
                r.RolId != rol.RolId);

        if (nombreExiste)
            return Result.Failure(NombreDuplicado);

        try
        {
            if (rol.RolId == 0)
            {
                // Se copian solo los campos propios, así no se guardan por accidente
                // los Usuarios que vengan anidados en el JSON.
                contexto.Roles.Add(new Role
                {
                    Nombre = nombre,
                    Descripcion = descripcion
                });
            }
            else
            {
                var existente = await contexto.Roles
                    .FirstOrDefaultAsync(r => r.RolId == rol.RolId);

                if (existente == null)
                    return Result.Failure(RolNoExiste);

                // El rol Administrador no se renombra. Como el nombre es único,
                // no puede existir otro rol Administrador que lo reemplace.
                if (existente.Nombre.Equals(RolAdministrador, StringComparison.OrdinalIgnoreCase) &&
                    !nombre.Equals(RolAdministrador, StringComparison.OrdinalIgnoreCase))
                    return Result.Failure(AdministradorNoRenombrable);

                existente.Nombre = nombre;
                existente.Descripcion = descripcion;
            }

            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    // Buscar rol por ID
    public async Task<Result<Role?>> Buscar(int rolId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var rol = await contexto.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RolId == rolId);

        return Result<Role?>.Success(rol);
    }

    // Obtener roles según un criterio
    public async Task<Result<List<Role>>> GetList(
        Expression<Func<Role, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var roles = await contexto.Roles
            .AsNoTracking()
            .Where(criterio)
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        return Result<List<Role>>.Success(roles);
    }

    // Buscar roles por nombre
    public async Task<Result<List<Role>>> BuscarPorNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<List<Role>>.Success(new List<Role>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        nombre = nombre.Trim();

        var roles = await contexto.Roles
            .AsNoTracking()
            .Where(r => r.Nombre.Contains(nombre))
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        return Result<List<Role>>.Success(roles);
    }

    // Buscar el rol asignado a un usuario
    public async Task<Result<Role?>> BuscarPorUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            return Result<Role?>.Success((Role?)null);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var rol = await contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.UsuarioId == usuarioId)
            .Select(u => u.Rol)
            .FirstOrDefaultAsync();

        return Result<Role?>.Success(rol);
    }

    // Buscar usuarios pertenecientes a un rol (sin devolver la contraseña)
    public async Task<Result<List<Usuario>>> BuscarUsuarios(int rolId)
    {
        if (rolId <= 0)
            return Result<List<Usuario>>.Success(new List<Usuario>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var usuarios = await contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.RolId == rolId)
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .ThenBy(u => u.UsuarioId)
            .Select(u => new Usuario
            {
                UsuarioId = u.UsuarioId,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                RolId = u.RolId,
                Activo = u.Activo,
                FechaRegistro = u.FechaRegistro
            })
            .ToListAsync();

        return Result<List<Usuario>>.Success(usuarios);
    }

    // Comprobar si un rol tiene usuarios asignados
    public async Task<Result<bool>> TieneUsuarios(int rolId)
    {
        if (rolId <= 0)
            return Result<bool>.Success(false);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool tieneUsuarios = await contexto.Usuarios
            .AnyAsync(u => u.RolId == rolId);

        return Result<bool>.Success(tieneUsuarios);
    }

    // Contar usuarios asignados a un rol
    public async Task<Result<int>> ContarUsuarios(int rolId)
    {
        if (rolId <= 0)
            return Result<int>.Success(0);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        int cantidad = await contexto.Usuarios
            .CountAsync(u => u.RolId == rolId);

        return Result<int>.Success(cantidad);
    }

    // Comprobar si existe al menos un usuario administrador activo
    public async Task<Result<bool>> ExisteAdministradorActivo()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        string administrador = RolAdministrador.ToLower();

        bool existe = await contexto.Usuarios
            .AnyAsync(u =>
                u.Activo &&
                u.Rol.Nombre.ToLower() == administrador);

        return Result<bool>.Success(existe);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    // No se eliminan roles con usuarios asignados ni el rol Administrador.
    public async Task<Result> Eliminar(int rolId)
    {
        if (rolId <= 0)
            return Result.Failure(RolNoExiste);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var rol = await contexto.Roles
            .FirstOrDefaultAsync(r => r.RolId == rolId);

        if (rol == null)
            return Result.Failure(RolNoExiste);

        if (rol.Nombre.Equals(RolAdministrador, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(AdministradorNoEliminable);

        bool tieneUsuarios = await contexto.Usuarios
            .AnyAsync(u => u.RolId == rolId);

        if (tieneUsuarios)
            return Result.Failure(RolConUsuarios);

        try
        {
            contexto.Roles.Remove(rol);
            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}