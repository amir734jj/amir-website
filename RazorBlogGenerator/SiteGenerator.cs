using Markdig;
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
        var services = new ServiceCollection();
        services.Scan(scan => scan
            .FromAssemblyOf<ContentPage>()
            .AddClasses(c => c.AssignableTo<ContentPage>())
            .AsSelf()
            .WithTransientLifetime());

        return services
            .Where(sd => sd.ServiceType != typeof(ContentPage) && typeof(ContentPage).IsAssignableFrom(sd.ServiceType))
            .ToDictionary(sd => sd.ServiceType.Name, sd => sd.ServiceType);
    }

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static async Task GenerateAsync(
        string dataDir,
        string templatesDir,
        string staticDir,
        string distDir)
    {
        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatesDir)
            .UseMemoryCachingProvider()
            .Build();

        var markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        if (Directory.Exists(distDir))
        {
            Directory.Delete(distDir, true);
        }

        Directory.CreateDirectory(distDir);

        var siteConfigPath = Path.Combine(dataDir, "site.yaml");
        var siteConfig = File.Exists(siteConfigPath)
            ? Deserializer.Deserialize<SiteConfig>(await File.ReadAllTextAsync(siteConfigPath))
            : new SiteConfig();

        var pages = new List<ContentPage>();

        foreach (var file in Directory.GetFiles(dataDir, "*.yaml", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file).Equals("site.yaml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var page = LoadPage(file, markdownPipeline);
            if (page != null)
            {
                pages.Add(page);
            }
        }

        foreach (var page in pages)
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

            var outputPath = page.Slug.Trim('/') is "" or "/"
                ? Path.Combine(distDir, "index.html")
                : Path.Combine(distDir, page.Slug.Trim('/'), "index.html");

            var dir = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(outputPath, fullHtml);
        }

        if (Directory.Exists(staticDir))
        {
            CopyDirectory(staticDir, Path.Combine(distDir, "assets"));
        }

        Log.Information("Generated {Count} pages to {DistDir}", pages.Count, distDir);
    }

    private static ContentPage? LoadPage(string filePath, MarkdownPipeline markdownPipeline)
    {
        var raw = File.ReadAllText(filePath);
        var parts = raw.Split("---", 2);
        var metaYaml = parts[0].Trim();
        var bodyRaw = parts.Length > 1 ? parts[1].Trim() : null;

        var meta = Deserializer.Deserialize<ContentPage>(metaYaml);

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

        ContentPage page;

        switch (meta.ContentType)
        {
            case ContentType.Markdown:
            {
                page = (ContentPage)Deserializer.Deserialize(metaYaml, modelType)!;

                if (page is PostModel post)
                {
                    // Look for companion .md file, fall back to body after ---
                    var mdPath = Path.ChangeExtension(filePath, ".md");
                    var markdown = File.Exists(mdPath)
                        ? File.ReadAllText(mdPath)
                        : bodyRaw;

                    if (!string.IsNullOrWhiteSpace(markdown))
                    {
                        post.RenderedHtml = Markdown.ToHtml(markdown, markdownPipeline);
                    }
                }

                break;
            }
            case ContentType.Yaml:
            {
                var yamlToDeserialize = string.IsNullOrWhiteSpace(bodyRaw) ? metaYaml : bodyRaw;
                page = (ContentPage)Deserializer.Deserialize(yamlToDeserialize, modelType)!;
                page.Model = meta.Model;
                page.Template = meta.Template;
                page.Slug = meta.Slug;
                page.Title = meta.Title;
                page.ContentType = meta.ContentType;
                break;
            }
            default:
                throw new ArgumentException("Unsupported meta type", nameof(meta));
        }

        return page;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var dest = Path.Combine(destination, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
