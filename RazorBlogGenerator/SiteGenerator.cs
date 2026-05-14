using System.Collections.Concurrent;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Markdig;
using RazorBlogGenerator.MarkdownExtensions;
using RazorBlogGenerator.Models;
using RazorLight;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RazorBlogGenerator;

public static class SiteGenerator
{
    private static readonly Dictionary<string, Type> ModelRegistry = BuildModelRegistry();

    private static Dictionary<string, Type> BuildModelRegistry()
    {
        return typeof(ContentPage).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ContentPage)) && !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly string[] YamlExtensions = [".yaml", ".yml"];
    private static readonly string[] ContentExtensions = [".md", ".markdown"];
    private static readonly string[] SiteConfigNames = ["site.yaml", "site.yml"];

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use(new ImgFluidExtension())
        .Build();

    private static readonly ConcurrentDictionary<string, Lazy<RazorLightEngine>> EngineCache = new();

    private static RazorLightEngine GetEngine(string templatesDir)
    {
        return EngineCache.GetOrAdd(templatesDir, dir => new Lazy<RazorLightEngine>(() =>
            new RazorLightEngineBuilder()
                .UseFileSystemProject(dir)
                .UseMemoryCachingProvider()
                .Build())).Value;
    }

    public static async Task GenerateAsync(
        string dataDir,
        string templatesDir,
        string distDir)
    {
        var engine = GetEngine(templatesDir);

        if (Directory.Exists(distDir))
        {
            Directory.Delete(distDir, true);
        }

        Directory.CreateDirectory(distDir);

        var siteConfigPath = SiteConfigNames
            .Select(n => Path.Combine(dataDir, n))
            .FirstOrDefault(File.Exists);
        var siteConfig = siteConfigPath is not null
            ? Deserializer.Deserialize<SiteConfig>(await File.ReadAllTextAsync(siteConfigPath))
            : new SiteConfig();

        var pages = Directory.GetFiles(dataDir, "*.*", SearchOption.AllDirectories)
            .Where(f => YamlExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Select(yamlFile => new { yamlFile, fileName = Path.GetFileName(yamlFile) })
            .Where(tuple => !SiteConfigNames.Any(n => tuple.fileName.Equals(n, StringComparison.OrdinalIgnoreCase)))
            .Select(tuple => LoadPage(tuple.yamlFile, dataDir, MarkdownPipeline))
            .Where(x => x is not null)
            .Cast<ContentPage>()
            .ToList();

        // Render all pages, then write in parallel
        var renderTasks = pages.Select(async page =>
        {
            var context = new RenderContext { Page = page, AllPages = pages, Site = siteConfig };
            var bodyHtml = await engine.CompileRenderAsync(page.Template, context);

            var layoutContext = new RenderContext
            {
                Page = page,
                AllPages = pages,
                Site = siteConfig,
                BodyHtml = bodyHtml
            };
            var fullHtml = await engine.CompileRenderAsync("Layout.cshtml", layoutContext);

            var outputPath = page.Route.Trim('/') is ""
                ? Path.Combine(distDir, "index.html")
                : Path.Combine(distDir, page.Route.Trim('/'), "index.html");

            return (outputPath, fullHtml);
        });

        var rendered = await Task.WhenAll(renderTasks);

        var writeTasks = rendered.Select(async r =>
        {
            var dir = Path.GetDirectoryName(r.outputPath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(r.outputPath, await FormatHtmlAsync(r.fullHtml));
        });

        await Task.WhenAll(writeTasks);

        CopyColocatedAssets(dataDir, distDir);

        Log.Information("Generated {Count} pages to {DistDir}", pages.Count, distDir);
    }

    private static async Task<string> FormatHtmlAsync(string html)
    {
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html);
        await using var writer = new StringWriter();
        document.ToHtml(writer, new PrettyMarkupFormatter());
        return writer.ToString();
    }

    private static ContentPage? LoadPage(string filePath, string dataDir, MarkdownPipeline markdownPipeline)
    {
        var yaml = File.ReadAllText(filePath);

        var meta = Deserializer.Deserialize<ContentPage>(yaml);

        if (string.IsNullOrWhiteSpace(meta.Model) || string.IsNullOrWhiteSpace(meta.Template))
        {
            Log.Warning("Skipping {FilePath}: missing 'model' or 'template'", filePath);
            return null;
        }

        if (!ModelRegistry.TryGetValue(meta.Model, out var modelType))
        {
            Log.Warning("Skipping {FilePath}: unknown model '{Model}'", filePath, meta.Model);
            return null;
        }

        var page = (ContentPage)Deserializer.Deserialize(yaml, modelType)!;
        page.Route = DeriveRoute(filePath, dataDir);

        if (page is PostModel post)
        {
            var contentDir = Path.GetDirectoryName(filePath)!;
            var mdFile = ContentExtensions
                .Select(ext => Path.Combine(contentDir, "content" + ext))
                .FirstOrDefault(File.Exists)
                ?? ContentExtensions
                    .Select(ext => Path.ChangeExtension(filePath, ext))
                    .FirstOrDefault(File.Exists);

            if (mdFile != null)
            {
                var markdown = File.ReadAllText(mdFile);
                var pipeline = page.Vars.Count > 0
                    ? new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .Use(new ImgFluidExtension())
                        .Use(new VarsExtension(page.Vars))
                        .Build()
                    : markdownPipeline;
                post.RenderedHtml = Markdown.ToHtml(markdown, pipeline);
            }
        }

        return page;
    }

    private static string DeriveRoute(string filePath, string dataDir)
    {
        var dir = Path.GetDirectoryName(filePath)!;
        var relative = Path.GetRelativePath(dataDir, dir).Replace('\\', '/').ToLowerInvariant();
        return relative == "." ? "/" : "/" + relative + "/";
    }

    private static void CopyColocatedAssets(string dataDir, string distDir)
    {
        foreach (var file in Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (YamlExtensions.Contains(ext) || ContentExtensions.Contains(ext))
            {
                continue;
            }

            var relative = Path.GetRelativePath(dataDir, file).Replace('\\', '/').ToLowerInvariant();
            var dest = Path.Combine(distDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
