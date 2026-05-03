using Microsoft.Extensions.FileProviders;
using Serilog;

namespace RazorBlogGenerator;

public static class StaticServer
{
    public static async Task RunAsync(string distDir, int port)
    {
        var fullPath = Path.GetFullPath(distDir);
        if (!Directory.Exists(fullPath))
        {
            Log.Error("Directory not found: {Path}", fullPath);
            Environment.ExitCode = 1;
            return;
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            WebRootPath = fullPath
        });
        builder.Logging.ClearProviders();

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "/";
            var lower = path.ToLowerInvariant();
            if (path != lower)
            {
                context.Request.Path = lower;
            }
            await next();
        });

        var fileProvider = new PhysicalFileProvider(fullPath);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ServeUnknownFileTypes = true
        });

        Log.Information("Serving {Path} at http://localhost:{Port}", fullPath, port);
        await app.RunAsync($"http://0.0.0.0:{port}");
    }
}
