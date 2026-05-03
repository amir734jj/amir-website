using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using Serilog;

namespace RazorBlogGenerator;

public static class DevServer
{
    private static IHubContext<LiveReloadHub>? _hubContext;
    private static readonly SemaphoreSlim RebuildLock = new(1, 1);

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
            if (!await RebuildLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                await RebuildAsync(dataDir, templatesDir, staticDir, distDir);
                if (_hubContext is not null)
                {
                    await _hubContext.Clients.All.SendAsync("Reload");
                }
            }
            finally
            {
                RebuildLock.Release();
            }
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
        builder.Services.AddSignalR();
        var app = builder.Build();

        _hubContext = app.Services.GetRequiredService<IHubContext<LiveReloadHub>>();

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ServeUnknownFileTypes = true
        });
        app.MapHub<LiveReloadHub>("/__livereload");

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
            await InjectLiveReloadAsync(distDir);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Rebuild failed");
        }
    }

    private static async Task InjectLiveReloadAsync(string distDir)
    {
        const string script = """
            <script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
            <script>
            (function(){
              var conn = new signalR.HubConnectionBuilder().withUrl("/__livereload").withAutomaticReconnect().build();
              conn.on("Reload", function(){ location.reload(); });
              conn.start();
            })();
            </script>
            """;

        foreach (var file in Directory.GetFiles(distDir, "*.html", SearchOption.AllDirectories))
        {
            var html = await File.ReadAllTextAsync(file);
            var injected = html.Replace("</body>", script + "</body>", StringComparison.OrdinalIgnoreCase);
            if (injected != html)
            {
                await File.WriteAllTextAsync(file, injected);
            }
        }
    }
}

 public class LiveReloadHub : Hub;

