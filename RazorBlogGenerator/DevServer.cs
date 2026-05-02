using Microsoft.Extensions.FileProviders;
using Serilog;

namespace RazorBlogGenerator;

public static class DevServer
{
    public static async Task RunAsync(string dataDir, string templatesDir, string staticDir, string distDir, int port)
    {
        await RebuildAsync(dataDir, templatesDir, staticDir, distDir);

        using var watcher = new FileSystemWatcher();
        var projectRoot = Path.GetDirectoryName(dataDir)!;
        watcher.Path = projectRoot;
        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
        watcher.Filter = "*.*";

        var debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
        debounceTimer.Elapsed += async (_, _) =>
        {
            await RebuildAsync(dataDir, templatesDir, staticDir, distDir);
        };

        watcher.Changed += (_, e) => OnChange(e, debounceTimer, distDir);
        watcher.Created += (_, e) => OnChange(e, debounceTimer, distDir);
        watcher.Deleted += (_, e) => OnChange(e, debounceTimer, distDir);
        watcher.Renamed += (_, e) => OnChange(e, debounceTimer, distDir);
        watcher.EnableRaisingEvents = true;

        Log.Information("Watching for changes in {Root}", projectRoot);
        Log.Information("Serving at http://localhost:{Port}", port);

        var fullDistPath = Path.GetFullPath(distDir);
        var fileProvider = new PhysicalFileProvider(fullDistPath);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            WebRootPath = fullDistPath
        });
        builder.Logging.ClearProviders();
        var app = builder.Build();

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ServeUnknownFileTypes = true
        });

        await app.RunAsync($"http://localhost:{port}");
    }

    private static void OnChange(FileSystemEventArgs e, System.Timers.Timer debounce, string distDir)
    {
        if (e.FullPath.Contains(Path.GetFullPath(distDir)))
        {
            return;
        }

        if (e.FullPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
            e.FullPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
        {
            return;
        }

        debounce.Stop();
        debounce.Start();
    }

    private static async Task RebuildAsync(string dataDir, string templatesDir, string staticDir, string distDir)
    {
        try
        {
            await SiteGenerator.GenerateAsync(dataDir, templatesDir, staticDir, distDir);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Rebuild failed");
        }
    }
}
