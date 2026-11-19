namespace GestionCirugiaPlastica.Blazor.Auth;

public sealed class UserProfileState
{
    public UserProfileDto? Current { get; private set; }
    public event Action? Changed;

    public void Set(UserProfileDto? profile)
    {
        Current = profile;
        Changed?.Invoke();
    }
}

public sealed class UserProfileDto
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string? NombreUsuario { get; set; }
    public string? Email { get; set; }
    public string? FotoUrl { get; set; }
    public int? PacienteId { get; set; }
    public string Rol { get; set; } = "";
    public string? AccessToken { get; set; }
}
