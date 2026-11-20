using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GestionCirugiaPlastica.Blazor;
using GestionCirugiaPlastica.Blazor.Auth;
using GestionCirugiaPlastica.Blazor.Layout;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<BearerTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7214/") };
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserProfileState>();
builder.Services.AddScoped<ThemeState>();

await builder.Build().RunAsync();
