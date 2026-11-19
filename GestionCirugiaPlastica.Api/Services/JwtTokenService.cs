using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.IdentityModel.Tokens;

namespace GestionCirugiaPlastica.Api.Services;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public IssuedJwtToken Create(Usuario user, string roleName)
    {
        var keyText = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(keyText) || Encoding.UTF8.GetByteCount(keyText) < 32)
            throw new InvalidOperationException("La clave Jwt:Key debe contener al menos 32 bytes.");

        var expiresAtUtc = DateTime.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UsuarioId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
            new(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}".Trim()),
            new(ClaimTypes.Role, roleName)
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
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new IssuedJwtToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}

public sealed record IssuedJwtToken(string AccessToken, DateTime ExpiresAtUtc);
