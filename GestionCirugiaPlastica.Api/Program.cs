using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Models;
using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization.Metadata;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la conexión con SQL Server
builder.Services.AddDbContextFactory<GestionCirugiaPlasticaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Registro de los servicios
builder.Services.AddScoped<IServiceResult<Paciente, int>, PacientesService>();
builder.Services.AddScoped<CirugiasService>();
builder.Services.AddScoped<IServiceResult<Cirugia, int>, CirugiasService>();
builder.Services.AddScoped<CitasService>();
builder.Services.AddScoped<ConsultasService>();
builder.Services.AddScoped<ControlesPostoperatoriosService>();
builder.Services.AddScoped<DetallesRecetaService>();
builder.Services.AddScoped<FotografiasService>();
builder.Services.AddScoped<GastosService>();
builder.Services.AddScoped<HistorialClinicoService>();
builder.Services.AddScoped<PacientesSegurosServices>();
builder.Services.AddScoped<HistorialEstadosCitasServices>();
builder.Services.AddScoped<PagosServices>();
builder.Services.AddScoped<PreoperatoriosServices>();
builder.Services.AddScoped<ProcedimientosServices>();
builder.Services.AddScoped<RecetasServices>();
builder.Services.AddScoped<RolesServices>();
builder.Services.AddScoped<SegurosServices>();
builder.Services.AddScoped<UsuariosServices>();

// Configuración de CORS para permitir la comunicación con Blazor
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7144",
            "http://localhost:5209"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Configuración de Controllers y OpenAPI
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrWhiteSpace(jwtKey) && Encoding.UTF8.GetByteCount(jwtKey) >= 32)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GestionCirugiaPlastica.Api",
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GestionCirugiaPlastica.Blazor",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
}
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// El generador de OpenAPI usa estas opciones (no las de MVC). Se quitan las propiedades
// de navegación de las entidades SOLO para el esquema, porque tienen referencias
// circulares (Receta <-> DetallesReceta, etc.) y rompen el documento con
// "CurrentDepth (64)". Las respuestas reales de la API no cambian.
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { QuitarNavegacionParaOpenApi }
    };
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GestionCirugiaPlasticaContext>>();
    await using var context = await factory.CreateDbContextAsync();
    context.Database.EnsureCreated();

    // Restablece las contraseñas de las cuentas de demostración con el mismo
    // PasswordHasher<Usuario> que valida AuthController durante el login.
    var demoPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["doctor.demo"] = "DoctorTest!2026",
        ["secretaria.demo"] = "SecretariaTest!2026",
        ["paciente.demo"] = "PacienteTest!2026"
    };
    var hasher = new PasswordHasher<Usuario>();
    var users = (await context.Usuarios
        .Where(u => u.NombreUsuario != null)
        .ToListAsync())
        .Where(u => demoPasswords.ContainsKey(u.NombreUsuario!.Trim()))
        .ToList();

    foreach (var user in users)
        user.PasswordHash = hasher.HashPassword(user, demoPasswords[user.NombreUsuario!.Trim()]);

    if (users.Count > 0)
        await context.SaveChangesAsync();
}

// Configuración de OpenAPI
app.MapOpenApi();

// Configuración de Scalar
app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/openapi/v1.json");
});

app.UseHttpsRedirection();

// Activar CORS
app.UseCors("BlazorPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void QuitarNavegacionParaOpenApi(JsonTypeInfo typeInfo)
{
    var espacioEntidades = typeof(Paciente).Namespace;

    if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.Type.Namespace != espacioEntidades)
        return;

    for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
    {
        var tipo = typeInfo.Properties[i].PropertyType;
        var tipoInterno = tipo.IsGenericType ? tipo.GetGenericArguments()[0] : tipo;

        if (tipoInterno.Namespace == espacioEntidades)
            typeInfo.Properties.RemoveAt(i);
    }
}
