using System.Net.Http.Headers;

namespace GestionCirugiaPlastica.WinForms;

/// <summary>
/// Sesión estática compartida para el cliente Windows Forms.
/// Mantiene el token JWT, los datos del usuario autenticado y su rol actual.
/// </summary>
internal static class UserSession
{
    private static readonly string[] AllowedRoles =
    [
        "Doctora",
        "AsistenteClinico",
        "Secretaria",
        "Paciente"
    ];

    public static string? AccessToken { get; private set; }
    public static DateTime? ExpiresAtUtc { get; private set; }
    public static string? UserName { get; private set; }
    public static string? Role { get; private set; }

    public static bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        (ExpiresAtUtc is null || ExpiresAtUtc > DateTime.UtcNow);

    public static bool IsDoctor => string.Equals(Role, "Doctora", StringComparison.OrdinalIgnoreCase);
    public static bool IsSecretary => string.Equals(Role, "Secretaria", StringComparison.OrdinalIgnoreCase);
    public static bool IsAssistant => string.Equals(Role, "AsistenteClinico", StringComparison.OrdinalIgnoreCase);
    public static bool IsPatient => string.Equals(Role, "Paciente", StringComparison.OrdinalIgnoreCase);
    public static bool IsClinicalStaff => IsDoctor || IsAssistant;

    public static bool IsSupportedRole(string? role) =>
        !string.IsNullOrWhiteSpace(role) &&
        AllowedRoles.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);

    public static void SetSession(string accessToken, DateTime? expiresAtUtc, string? userName, string? role)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("El token JWT no puede estar vacío.", nameof(accessToken));

        AccessToken = accessToken.Trim();
        ExpiresAtUtc = expiresAtUtc;
        UserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
    }

    public static void UpdateToken(string accessToken, DateTime? expiresAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        AccessToken = accessToken.Trim();
        if (expiresAtUtc.HasValue)
            ExpiresAtUtc = expiresAtUtc;
    }

    public static void UpdateDisplayName(string? userName) =>
        UserName = string.IsNullOrWhiteSpace(userName) ? UserName : userName.Trim();

    public static AuthenticationHeaderValue? CreateAuthorizationHeader() =>
        string.IsNullOrWhiteSpace(AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", AccessToken);

    public static void Clear()
    {
        AccessToken = null;
        ExpiresAtUtc = null;
        UserName = null;
        Role = null;
    }
}
