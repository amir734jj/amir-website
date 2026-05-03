using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;
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

        var pages = new List<ContentPage>();

        foreach (var yamlFile in Directory.GetFiles(dataDir, "*.*", SearchOption.AllDirectories)
            .Where(f => YamlExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))))
        {
            var fileName = Path.GetFileName(yamlFile);
            if (SiteConfigNames.Any(n => fileName.Equals(n, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var page = LoadPage(yamlFile, dataDir, MarkdownPipeline);
            if (page != null)
            {
                pages.Add(page);
            }
        }

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
                post.RenderedHtml = Markdown.ToHtml(markdown, markdownPipeline);
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

file sealed class ImgFluidExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline) { }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            var existing = htmlRenderer.ObjectRenderers.FindExact<LinkInlineRenderer>();
            if (existing != null)
            {
                htmlRenderer.ObjectRenderers.Remove(existing);
            }

            htmlRenderer.ObjectRenderers.AddIfNotAlready(new ImgFluidLinkInlineRenderer());
        }
    }
}

file sealed class ImgFluidLinkInlineRenderer : LinkInlineRenderer
{
    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        if (link.IsImage)
        {
            link.GetAttributes().AddClass("img-fluid");
        }

        base.Write(renderer, link);
    }
}
