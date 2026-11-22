using System.Text.Json;

namespace GestionCirugiaPlastica.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var baseUrl = "https://localhost:7214/";
        var fallbackBaseUrl = "http://localhost:5258/";
        if (File.Exists(settingsPath))
        {
            using var config = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (config.RootElement.TryGetProperty("Api", out var api))
            {
                if (api.TryGetProperty("BaseUrl", out var url) && !string.IsNullOrWhiteSpace(url.GetString()))
                    baseUrl = url.GetString()!;
                if (api.TryGetProperty("FallbackBaseUrl", out var fallback) && !string.IsNullOrWhiteSpace(fallback.GetString()))
                    fallbackBaseUrl = fallback.GetString()!;
            }
        }
        static Uri EnsureTrailingSlash(string raw) =>
            new(raw.EndsWith('/') ? raw : raw + "/");

        Application.Run(new MainForm(new ClinicApiClient(
            EnsureTrailingSlash(baseUrl),
            EnsureTrailingSlash(fallbackBaseUrl))));
    }
}
