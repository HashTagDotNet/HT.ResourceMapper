using ResourceMapper.Common.Server.Utils;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.RegisterCommonDependencies();

builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseBlazorFrameworkFiles();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowBlazorWasm");

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
