using System.Net.Http.Json;
using System.Text.Json;

namespace GestionCirugiaPlastica.Blazor.Auth;

public sealed class AuthService(HttpClient http, JwtAuthenticationStateProvider stateProvider)
{
    public async Task<(bool Success, string Message)> LoginAsync(string identifier, string password)
    {
        using var response = await http.PostAsJsonAsync("api/Auth/login", new LoginRequest(identifier, password));
        if (!response.IsSuccessStatusCode)
        {
            var message = "Usuario o contraseña incorrectos.";
            try
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (body.RootElement.TryGetProperty("message", out var m)) message = m.GetString() ?? message;
            }
            catch (JsonException) { }
            return (false, message);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result is null || string.IsNullOrWhiteSpace(result.AccessToken))
            return (false, "La respuesta de autenticación no es válida.");
        await stateProvider.SetTokenAsync(result.AccessToken);
        return (true, "");
    }

    public Task LogoutAsync() => stateProvider.ClearTokenAsync();

    private sealed record LoginRequest(string Identifier, string Password);
    private sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Name, string Role);
}
