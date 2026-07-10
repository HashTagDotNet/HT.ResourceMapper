using MudBlazor.Services;
using ResourceMapper.Common.Server.Utils;
using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.UI.Web.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Blazor Web App: static SSR globally, with interactive-server islands opted in per component.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Circuit-scoped nested-editor nav stack (slice #9) — per-circuit so it survives route changes
// within a connection but is cleared on refresh/reconnect (design: "refresh mid-stack loses the
// in-progress stack").
builder.Services.AddScoped<EditorNavStack>();

// API controllers (e.g. POST /api/resources/import for external/Bruno use; the UI calls services in-process).
builder.Services.AddControllers();

// Reuse the shared backend (GlobalConfig, IDbConnector, repositories, services).
builder.Services.RegisterCommonDependencies();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
