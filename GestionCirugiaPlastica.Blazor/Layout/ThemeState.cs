using Microsoft.JSInterop;

namespace GestionCirugiaPlastica.Blazor.Layout;

public sealed class ThemeState
{
    private bool _initialized;

    public bool IsDark { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync(IJSRuntime js)
    {
        if (_initialized) return;
        _initialized = true;
        try { IsDark = await js.InvokeAsync<string?>("localStorage.getItem", "tanger-theme") == "dark"; }
        catch (JSException) { IsDark = false; }
        Changed?.Invoke();
    }

    public async Task SetDarkAsync(bool isDark, IJSRuntime js)
    {
        IsDark = isDark;
        try { await js.InvokeVoidAsync("localStorage.setItem", "tanger-theme", isDark ? "dark" : "light"); }
        catch (JSException) { }
        Changed?.Invoke();
    }
}
