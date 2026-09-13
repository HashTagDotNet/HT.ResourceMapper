using HT.Microsoft.IConfiguration.Extensions;
using MudBlazor.Services;
using ResourceMapper.Common.Server.Utils;
using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.UI.Web.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// User secrets are added explicitly rather than left to CreateBuilder, which adds them ONLY when the
// environment is literally named "Development". This machine runs as "localhost" (as do the sibling
// MacroPoint repos, each with its own appsettings.localhost.json), so depending on that default made
// the app start under F5 and fail from a shell — same code, same machine, bare 500 on every page
// because the connection string was simply absent. Loading secrets by name instead of by environment
// removes the dependency on what the environment happens to be called.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Re-add the two highest-precedence sources. Appending user secrets above put them ABOVE environment
// variables and command-line args, which inverts the conventional order and would make a deploy-time
// override silently lose to a developer's local secret file. Re-adding these restores it.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// Fail fast, and name the environment while doing it. Previously a missing key surfaced as a 500 on
// every page with the reason only in the log file. The environment name is almost always what explains
// the miss — both the appsettings overlay and user secrets are selected by it — so it belongs in the
// message rather than being left to be inferred.
try
{
    builder.Configuration.ValidateRequiredKeys(
        "ResourceMapper:ConnectionStrings:Database:RO",
        "ResourceMapper:ConnectionStrings:Database:RW");
}
catch (ArgumentException ex)
{
    var env = builder.Environment.EnvironmentName;
    throw new InvalidOperationException(
        $"{ex.Message}. Resolved environment is '{env}' — expected appsettings.{env}.json to supply " +
        $"them, or user secrets for this project. Note DOTNET_ENVIRONMENT overrides " +
        $"ASPNETCORE_ENVIRONMENT when both are set.", ex);
}

// Static Web Assets (MudBlazor's _content/*, _framework/blazor.web.js, the scoped-CSS bundle) are
// wired up automatically ONLY when the environment is literally named "Development". Under any other
// name, a non-published `dotnet run` serves 500 for every one of them: the page renders unstyled and
// the Blazor circuit never starts, so the UI looks fine in a screenshot but nothing is clickable.
// Enabling them explicitly is what the framework's own warning recommends. No-ops against published
// output, where the assets are on disk already.
builder.WebHost.UseStaticWebAssets();

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

// "localhost" is a local-development environment here just as "Development" is — it is the name this
// machine and the sibling repos use. Testing only IsDevelopment() swapped the developer exception page
// for the generic /Error handler during local runs, which is how a wall of static-asset 500s showed up
// as a silently dead UI instead of a stack trace.
var isLocalDevelopment = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("localhost");

if (!isLocalDevelopment)
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
