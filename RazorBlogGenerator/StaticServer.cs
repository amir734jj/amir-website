using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Serilog;

namespace RazorBlogGenerator;

public static class StaticServer
{
    public static async Task RunAsync(string distDir, int port)
    {
        var fullPath = Path.GetFullPath(distDir);
        if (!Directory.Exists(fullPath) || Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories).Length == 0)
        {
            Log.Error("Directory not found or empty: {Path}", fullPath);
            Environment.ExitCode = 1;
            return;
        }

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        var app = builder.Build();

        ConfigureStaticServing(app, fullPath);

        Log.Information("Serving {Path} at http://localhost:{Port}", fullPath, port);
        await app.RunAsync($"http://0.0.0.0:{port}");
    }

    public static void ConfigureStaticServing(WebApplication app, string fullPath)
    {
        var fileProvider = new PhysicalFileProvider(fullPath);

        // Lowercase all paths + rewrite directory requests to index.html
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "/";
            var lower = path.ToLowerInvariant();
            if (path != lower)
            {
                context.Request.Path = lower;
                path = lower;
            }

            if (path.EndsWith('/'))
            {
                var candidate = path + "index.html";
                if (fileProvider.GetFileInfo(candidate).Exists)
                {
                    context.Request.Path = candidate;
                }
            }
            else if (!Path.HasExtension(path))
            {
                var candidate = path + "/index.html";
                if (fileProvider.GetFileInfo(candidate).Exists)
                {
                    context.Response.Redirect(path + "/", permanent: false);
                    return;
                }
            }

            await next();
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ServeUnknownFileTypes = true
        });

        app.Run(async context =>
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync($"404 - Not Found: {context.Request.Path}");
        });
    }
}
