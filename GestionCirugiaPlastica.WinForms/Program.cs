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
        if (File.Exists(settingsPath))
        {
            using var config = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (config.RootElement.TryGetProperty("Api", out var api) && api.TryGetProperty("BaseUrl", out var url))
                baseUrl = url.GetString() ?? baseUrl;
        }
        Application.Run(new MainForm(new ClinicApiClient(new Uri(baseUrl))));
    }
}
