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
    public bool IsDoctor => string.Equals(Role, "Doctor", StringComparison.OrdinalIgnoreCase);

    public async Task LoginAsync(string identifier, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/Auth/login", new { identifier, password });
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

    public void Logout() { _http.DefaultRequestHeaders.Authorization = null; Role = null; UserName = null; }

    public async Task<T> GetAsync<T>(string path)
    {
        using var response = await _http.GetAsync(path);
        var text = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        if (!response.IsSuccessStatusCode) throw ApiError(response.StatusCode, document.RootElement);
        var root = document.RootElement;
        var value = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var wrapped) ? wrapped : root;
        return value.Deserialize<T>(Json) ?? throw new InvalidOperationException("La respuesta de la API está vacía o no tiene el formato esperado.");
    }

    public async Task SendAsync(HttpMethod method, string path, object payload)
    {
        using var response = await _http.SendAsync(new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) });
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            throw ApiError(response.StatusCode, document.RootElement);
        }
    }

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

internal sealed class ApiPaciente
{
    public int PacienteId { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Cedula { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public bool TieneSeguro { get; set; }
    public string? Sexo { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public DateTime FechaRegistro { get; set; }
}
internal sealed class ApiCita
{
    public int CitaId { get; set; }
    public DateTime Fecha { get; set; }
    public int PacienteId { get; set; }
    public ApiPaciente? Paciente { get; set; }
    public string? TipoCita { get; set; }
    public decimal Costo { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string EstadoPago { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
    public int? CitaAnteriorId { get; set; }
}
internal sealed class ApiCirugia
{
    public int CirugiaId { get; set; }
    public DateTime FechaCirugia { get; set; }
    public int PacienteId { get; set; }
    public ApiPaciente? Paciente { get; set; }
    public int ProcedimientoId { get; set; }
    public ApiProcedimiento? Procedimiento { get; set; }
    public string? DiagnosticoPreoperatorio { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "";
}
internal sealed class ApiProcedimiento { public string Nombre { get; set; } = ""; }
internal sealed class ApiPago
{
    public int PagoId { get; set; }
    public DateTime Fecha { get; set; }
    public int? CirugiaId { get; set; }
    public int? CitaId { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Concepto { get; set; }
    public ApiCirugia? Cirugia { get; set; }
    public ApiCita? Cita { get; set; }
}
