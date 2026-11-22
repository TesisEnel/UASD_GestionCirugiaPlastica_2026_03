using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Text.Json;

namespace GestionCirugiaPlastica.WinForms;

internal sealed class ClinicApiClient : IDisposable
{
    private readonly HttpClient _http;
    private Uri _activeBaseAddress;
    private readonly Uri? _fallbackBaseAddress;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public ClinicApiClient(Uri baseAddress, Uri? fallbackBaseAddress = null)
    {
        _activeBaseAddress = baseAddress;
        _fallbackBaseAddress = fallbackBaseAddress;

        var httpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, _, _, sslPolicyErrors) =>
            {
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

                var host = request.RequestUri?.Host;
                return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
            }
        };

        var authHandler = new BearerSessionHandler(httpHandler);
        _http = new HttpClient(authHandler, disposeHandler: true)
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromSeconds(20)
        };

        SyncDefaultAuthorizationHeader();
    }

    public string? AccessToken => UserSession.AccessToken;
    public string? Role => UserSession.Role;
    public string? UserName => UserSession.UserName;
    public Uri BaseAddress => _activeBaseAddress;
    public bool IsDoctor => UserSession.IsDoctor;
    public bool IsSecretary => UserSession.IsSecretary;
    public bool IsAssistant => UserSession.IsAssistant;
    public bool IsPatient => UserSession.IsPatient;
    public bool IsClinicalStaff => UserSession.IsClinicalStaff;

    public async Task LoginAsync(string identifier, string password)
    {
        var normalizedIdentifier = identifier?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(normalizedIdentifier) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Ingresa tu usuario (o correo) y contraseña.");

        var requestPayload = new LoginRequestDto(normalizedIdentifier, password);
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(BuildUri("api/Auth/login"), requestPayload);
        }
        catch (HttpRequestException ex) when (CanFallback())
        {
            try
            {
                response = await _http.PostAsJsonAsync(new Uri(_fallbackBaseAddress!, "api/Auth/login"), requestPayload);
                _activeBaseAddress = _fallbackBaseAddress!;
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException(
                    $"No se pudo conectar con la API en {_activeBaseAddress} ni en {_fallbackBaseAddress}. Inicia GestionCirugiaPlastica.Api y confirma que escucha en uno de esos puertos.",
                    ex);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"No se pudo conectar con la API en {_activeBaseAddress}. Inicia GestionCirugiaPlastica.Api y confirma que escucha en esa dirección.",
                ex);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(ExtractErrorMessage(text, response.StatusCode) ?? "No fue posible iniciar sesión.");

            LoginResponseDto? login;
            try
            {
                login = JsonSerializer.Deserialize<LoginResponseDto>(text, Json);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("La respuesta del servidor de autenticación no tiene el formato JSON esperado.", ex);
            }

            if (login is null || string.IsNullOrWhiteSpace(login.AccessToken))
                throw new InvalidOperationException("La API no devolvió un token JWT válido.");

            if (!UserSession.IsSupportedRole(login.Role))
                throw new InvalidOperationException($"El rol '{login.Role ?? "(sin rol)"}' no está habilitado para acceder a esta aplicación.");

            UserSession.SetSession(login.AccessToken, login.ExpiresAtUtc, login.Name, login.Role);
            SyncDefaultAuthorizationHeader();
        }
    }

    public void Logout()
    {
        UserSession.Clear();
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<T> GetAsync<T>(string path)
    {
        SyncDefaultAuthorizationHeader();
        using var response = await _http.GetAsync(BuildUri(path));
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw ApiError(response.StatusCode, text);

        using var document = ParseJsonOrEmpty(text);
        var root = document.RootElement;
        var wrapped = root.ValueKind == JsonValueKind.Object
            ? root.EnumerateObject().FirstOrDefault(property => string.Equals(property.Name, "value", StringComparison.OrdinalIgnoreCase)).Value
            : default;
        var value = wrapped.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null ? wrapped : root;
        return value.Deserialize<T>(Json) ?? throw new InvalidOperationException("La respuesta de la API está vacía o no tiene el formato esperado.");
    }

    public async Task SendAsync(HttpMethod method, string path, object payload)
    {
        SyncDefaultAuthorizationHeader();
        using var request = new HttpRequestMessage(method, BuildUri(path)) { Content = JsonContent.Create(payload) };
        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw ApiError(response.StatusCode, text);
    }

    public async Task<T> SendForJsonAsync<T>(HttpMethod method, string path, object payload)
    {
        SyncDefaultAuthorizationHeader();
        using var request = new HttpRequestMessage(method, BuildUri(path)) { Content = JsonContent.Create(payload) };
        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw ApiError(response.StatusCode, text);

        using var document = ParseJsonOrEmpty(text);
        return document.RootElement.Deserialize<T>(Json) ?? throw new InvalidOperationException("La API no devolvió los datos actualizados.");
    }

    public async Task PatchAsync(string path)
    {
        SyncDefaultAuthorizationHeader();
        using var response = await _http.PatchAsync(BuildUri(path), null);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw ApiError(response.StatusCode, text);
    }

    public void SetAccessToken(string token)
    {
        UserSession.UpdateToken(token);
        SyncDefaultAuthorizationHeader();
    }

    public void SetDisplayName(string name) => UserSession.UpdateDisplayName(name);

    public async Task DeleteAsync(string path)
    {
        SyncDefaultAuthorizationHeader();
        using var response = await _http.DeleteAsync(BuildUri(path));
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw ApiError(response.StatusCode, text);
    }

    public async Task SendFileAsync(string path, string filePath, string fieldName = "file")
    {
        SyncDefaultAuthorizationHeader();
        using var content = new MultipartFormDataContent();
        await using var stream = File.OpenRead(filePath);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MimeType(filePath));
        content.Add(fileContent, fieldName, Path.GetFileName(filePath));
        using var response = await _http.PostAsync(BuildUri(path), content);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw ApiError(response.StatusCode, text);
    }

    private Uri BuildUri(string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out var absolute) ? absolute : new Uri(_activeBaseAddress, path);

    private bool CanFallback() =>
        _fallbackBaseAddress is not null &&
        !Uri.Equals(_activeBaseAddress, _fallbackBaseAddress);

    private void SyncDefaultAuthorizationHeader() =>
        _http.DefaultRequestHeaders.Authorization = UserSession.CreateAuthorizationHeader();

    private static string MimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private static JsonDocument ParseJsonOrEmpty(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return JsonDocument.Parse("{}");

        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("{}");
        }
    }

    private static Exception ApiError(HttpStatusCode status, string responseBody)
    {
        var detail = ExtractErrorMessage(responseBody, status);
        return new InvalidOperationException(status switch
        {
            HttpStatusCode.Unauthorized => detail ?? "Sesión vencida o credenciales inválidas. Inicia sesión de nuevo.",
            HttpStatusCode.Forbidden => detail ?? "Tu usuario no tiene permiso para esta operación.",
            HttpStatusCode.NotFound => detail ?? "No se encontró el registro solicitado.",
            HttpStatusCode.Conflict => detail ?? "La operación no se puede completar por registros relacionados.",
            _ => detail ?? $"La API respondió {(int)status} ({status})."
        });
    }

    private static string? ExtractErrorMessage(string responseBody, HttpStatusCode status)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return status == HttpStatusCode.Unauthorized ? "Sesión vencida o credenciales inválidas." : null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            return ReadMessage(doc.RootElement);
        }
        catch (JsonException)
        {
            var trimmed = responseBody.Trim();
            return trimmed.Length > 300 ? trimmed[..300] : trimmed;
        }
    }

    private static string? ReadMessage(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String)
            return root.GetString();

        if (root.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var key in new[] { "message", "detail", "title" })
        {
            if (TryGetPropertyIgnoreCase(root, key, out var item) && item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                return item.GetString();
        }

        if (TryGetPropertyIgnoreCase(root, "error", out var error))
        {
            if (error.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(error.GetString()))
                return error.GetString();

            if (error.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "description", "message", "code" })
                {
                    if (TryGetPropertyIgnoreCase(error, key, out var desc) && desc.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(desc.GetString()))
                        return desc.GetString();
                }
            }
        }

        if (TryGetPropertyIgnoreCase(root, "errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
        {
            var messages = errors.EnumerateObject()
                .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray() : Enumerable.Empty<JsonElement>())
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x));
            var joined = string.Join(" ", messages);
            if (!string.IsNullOrWhiteSpace(joined))
                return joined;
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public void Dispose() => _http.Dispose();

    private sealed record LoginRequestDto(string Identifier, string Password);

    private sealed class LoginResponseDto
    {
        public string AccessToken { get; set; } = "";
        public DateTime ExpiresAtUtc { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
    }

    private sealed class BearerSessionHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var isAnonymousAuthEndpoint =
                path.EndsWith("/api/Auth/login", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/api/Auth/register", StringComparison.OrdinalIgnoreCase);

            if (!isAnonymousAuthEndpoint && UserSession.CreateAuthorizationHeader() is { } bearer)
                request.Headers.Authorization = bearer;

            return base.SendAsync(request, cancellationToken);
        }
    }
}
