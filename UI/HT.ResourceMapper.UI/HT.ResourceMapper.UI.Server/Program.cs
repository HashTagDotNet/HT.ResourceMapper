using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace HT.ResourceMapper.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            
            // This serves the WebAssembly framework files (_framework/*)
            app.UseBlazorFrameworkFiles();
            // This serves static files from wwwroot folders
            app.UseStaticFiles();
            
            app.UseRouting();

            // Fallback for client-side routing - this is crucial for WebAssembly
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
