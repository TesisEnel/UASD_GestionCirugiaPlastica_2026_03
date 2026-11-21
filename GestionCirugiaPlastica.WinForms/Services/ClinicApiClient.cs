using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GestionCirugiaPlastica.WinForms;

internal sealed class ClinicApiClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    public ClinicApiClient(Uri baseAddress) => _http = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(20) };
    public string? Role { get; private set; }
    public string? UserName { get; private set; }
    public Uri? BaseAddress => _http.BaseAddress;
    public bool IsDoctor => string.Equals(Role, "Doctora", StringComparison.OrdinalIgnoreCase);
    public bool IsSecretary => string.Equals(Role, "Secretaria", StringComparison.OrdinalIgnoreCase);
    public bool IsAssistant => string.Equals(Role, "AsistenteClinico", StringComparison.OrdinalIgnoreCase);
    public bool IsPatient => string.Equals(Role, "Paciente", StringComparison.OrdinalIgnoreCase);
    public bool IsClinicalStaff => IsDoctor || string.Equals(Role, "AsistenteClinico", StringComparison.OrdinalIgnoreCase);

    public async Task LoginAsync(string identifier, string password)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("api/Auth/login", new { identifier, password });
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"No se pudo conectar con la API en {_http.BaseAddress}. Inicia GestionCirugiaPlastica.Api y confirma que escucha en esa direccion.", ex);
        }
        using (response)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(ReadMessage(body.RootElement) ?? "No fue posible iniciar sesión.");
            var root = body.RootElement;
            var token = root.GetProperty("accessToken").GetString();
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("La API no devolvió un token válido.");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Role = root.GetProperty("role").GetString();
            UserName = root.GetProperty("name").GetString();
        }
    }

    public void Logout() { _http.DefaultRequestHeaders.Authorization = null; Role = null; UserName = null; }

    public async Task<T> GetAsync<T>(string path)
    {
        using var response = await _http.GetAsync(path);
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        if (!response.IsSuccessStatusCode) throw ApiError(response.StatusCode, document.RootElement);
        var root = document.RootElement;
        var wrapped = root.ValueKind == JsonValueKind.Object
            ? root.EnumerateObject().FirstOrDefault(property => string.Equals(property.Name, "value", StringComparison.OrdinalIgnoreCase)).Value
            : default;
        var value = wrapped.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null ? wrapped : root;
        return value.Deserialize<T>(Json) ?? throw new InvalidOperationException("La respuesta de la API está vacía o no tiene el formato esperado.");
    }

    public async Task SendAsync(HttpMethod method, string path, object payload)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };
        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            throw ApiError(response.StatusCode, document.RootElement);
        }
    }

    public async Task<T> SendForJsonAsync<T>(HttpMethod method, string path, object payload)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };
        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        if (!response.IsSuccessStatusCode) throw ApiError(response.StatusCode, document.RootElement);
        return document.RootElement.Deserialize<T>(Json) ?? throw new InvalidOperationException("La API no devolvió los datos actualizados.");
    }

    public async Task PatchAsync(string path)
    {
        using var response = await _http.PatchAsync(path, null);
        var text = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) return;
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        throw ApiError(response.StatusCode, document.RootElement);
    }

    public void SetAccessToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    public void SetDisplayName(string name) => UserName = name;

    public async Task DeleteAsync(string path)
    {
        using var response = await _http.DeleteAsync(path);
        var text = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) return;
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        throw ApiError(response.StatusCode, document.RootElement);
    }

    public async Task SendFileAsync(string path, string filePath, string fieldName = "file")
    {
        using var content = new MultipartFormDataContent();
        await using var stream = File.OpenRead(filePath);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MimeType(filePath));
        content.Add(fileContent, fieldName, Path.GetFileName(filePath));
        using var response = await _http.PostAsync(path, content);
        var text = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) return;
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        throw ApiError(response.StatusCode, document.RootElement);
    }

    private static string MimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", _ => "application/octet-stream"
    };

    private static Exception ApiError(HttpStatusCode status, JsonElement root)
    {
        var detail = ReadMessage(root);
        return new InvalidOperationException(status switch
        {
            HttpStatusCode.Unauthorized => "Sesión vencida o credenciales inválidas. Inicia sesión de nuevo.",
            HttpStatusCode.Forbidden => "Tu usuario no tiene permiso para esta operación.",
            HttpStatusCode.NotFound => detail ?? "No se encontró el registro solicitado.",
            HttpStatusCode.Conflict => detail ?? "La operación no se puede completar por registros relacionados.",
            _ => detail ?? $"La API respondió {(int)status} ({status})."
        });
    }

    private static string? ReadMessage(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String) return root.GetString();
        foreach (var key in new[] { "message", "detail", "title", "error" })
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String) return item.GetString();
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("errors", out var errors)) return errors.ToString();
        return null;
    }
    public void Dispose() => _http.Dispose();
}
