using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace GestionCirugiaPlastica.Blazor.Auth;

public sealed class JwtAuthenticationStateProvider(IJSRuntime js) : AuthenticationStateProvider
{
    private const string TokenKey = "authToken";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
        return new AuthenticationState(CreatePrincipal(token));
    }

    public async Task SetTokenAsync(string token)
    {
        await js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public ValueTask<string?> TokenAsync() => js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);

    public async Task ClearTokenAsync()
    {
        await js.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static ClaimsPrincipal CreatePrincipal(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return new ClaimsPrincipal(new ClaimsIdentity());
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return new ClaimsPrincipal(new ClaimsIdentity());
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
            var claims = new List<Claim>();
            foreach (var property in json.RootElement.EnumerateObject())
            {
                var claimType = property.Name switch
                {
                    "role" => ClaimTypes.Role,
                    "unique_name" => ClaimTypes.Name,
                    "nameid" => ClaimTypes.NameIdentifier,
                    _ => property.Name
                };
                if (property.Value.ValueKind == JsonValueKind.Array)
                    claims.AddRange(property.Value.EnumerateArray().Select(v => new Claim(claimType, v.ToString())));
                else if (property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    claims.Add(new Claim(claimType, property.Value.ToString()));
            }
            if (json.RootElement.TryGetProperty("exp", out var exp) && exp.GetInt64() <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return new ClaimsPrincipal(new ClaimsIdentity());
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}

public sealed class BearerTokenHandler(JwtAuthenticationStateProvider auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetTokenAsync()
    {
        // Obtiene el mismo valor que mantiene el estado de autenticación.
        return await auth.TokenAsync();
    }
}
