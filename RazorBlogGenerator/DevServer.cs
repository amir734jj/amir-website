using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace RazorBlogGenerator;

public static class DevServer
{
    private static IHubContext<LiveReloadHub>? _hubContext;
    private static readonly SemaphoreSlim RebuildLock = new(1, 1);

    public static async Task RunAsync(string dataDir, string templatesDir, string distDir, int port)
    {
        await RebuildAsync(dataDir, templatesDir, distDir);

        var fullDistPath = Path.GetFullPath(distDir);

        using var dataWatcher = CreateWatcher(dataDir, distDir);
        using var templatesWatcher = CreateWatcher(templatesDir, distDir);

        var debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
        debounceTimer.Elapsed += async (_, _) =>
        {
            if (!await RebuildLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                await RebuildAsync(dataDir, templatesDir, distDir);
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

        Hook(dataWatcher);
        Hook(templatesWatcher);

        Log.Information("Watching {DataDir} and {TemplatesDir}", dataDir, templatesDir);
        Log.Information("Serving at http://localhost:{Port}", port);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSignalR();
        var app = builder.Build();

        _hubContext = app.Services.GetRequiredService<IHubContext<LiveReloadHub>>();

        app.MapHub<LiveReloadHub>("/__livereload");

        StaticServer.ConfigureStaticServing(app, fullDistPath);

        await app.RunAsync($"http://localhost:{port}");
        return;

        void Hook(FileSystemWatcher w)
        {
            w.Changed += (_, e) => OnChange(e, debounceTimer, fullDistPath);
            w.Created += (_, e) => OnChange(e, debounceTimer, fullDistPath);
            w.Deleted += (_, e) => OnChange(e, debounceTimer, fullDistPath);
            w.Renamed += (_, e) => OnChange(e, debounceTimer, fullDistPath);
            w.EnableRaisingEvents = true;
        }
    }

    private static FileSystemWatcher CreateWatcher(string dir, string distDir) => new()
    {
        Path = dir,
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
        Filter = "*.*",
    };

    private static void OnChange(FileSystemEventArgs e, System.Timers.Timer debounce, string fullDistPath)
    {
        if (e.FullPath.StartsWith(fullDistPath, StringComparison.Ordinal))
        {
            return;
        }

        debounce.Stop();
        debounce.Start();
    }

    private static async Task RebuildAsync(string dataDir, string templatesDir, string distDir)
    {
        try
        {
            await SiteGenerator.GenerateAsync(dataDir, templatesDir, distDir);
            await InjectLiveReloadAsync(distDir);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Rebuild failed");
        }
    }

    private static async Task InjectLiveReloadAsync(string distDir)
    {
        var parser = new HtmlParser();

        foreach (var file in Directory.GetFiles(distDir, "*.html", SearchOption.AllDirectories))
        {
            var rawHtml = await File.ReadAllTextAsync(file);
            var (htmlNoPre, preBlocks) = SiteGenerator.ExtractPreBlocks(rawHtml);

            var document = await parser.ParseDocumentAsync(htmlNoPre);
            var body = document.Body;
            if (body == null)
            {
                continue;
            }

            var signalrScript = document.CreateElement("script");
            signalrScript.SetAttribute("src", "https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js");
            body.AppendChild(signalrScript);

            var reloadScript = document.CreateElement("script");
            reloadScript.TextContent = """
                (function(){
                  var conn = new signalR.HubConnectionBuilder().withUrl("/__livereload").withAutomaticReconnect().build();
                  conn.on("Reload", function(){ location.reload(); });
                  conn.start();
                })();
                """;
            body.AppendChild(reloadScript);

            await using var writer = new StringWriter();
            document.ToHtml(writer, new PrettyMarkupFormatter());
            await File.WriteAllTextAsync(file, SiteGenerator.RestorePreBlocks(writer.ToString(), preBlocks));
        }
    }
}

 public class LiveReloadHub : Hub;

