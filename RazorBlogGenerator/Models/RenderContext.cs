namespace RazorBlogGenerator.Models;

public class RenderContext
{
    public required ContentPage Page { get; init; }
    public required IReadOnlyList<ContentPage> AllPages { get; init; }
    public required SiteConfig Site { get; init; }
    public string BodyHtml { get; init; } = string.Empty;

    public IReadOnlyList<PostModel> GetAllDescendants()
    {
        var currentRoute = Page.Route.TrimEnd('/') + "/";
        var hiddenIndexRoutes = AllPages
            .OfType<IndexModel>()
            .Where(p => p.Hidden && p.Route != Page.Route)
            .Select(p => p.Route.TrimEnd('/') + "/")
            .ToHashSet();

        return AllPages
            .OfType<PostModel>()
            .Where(p => !p.Hidden
                && p.Route != Page.Route
                && p.Route.StartsWith(currentRoute)
                && !hiddenIndexRoutes.Any(hr => p.Route.StartsWith(hr)))
            .OrderByDescending(p => p.PublishedOn)
            .ToList();
    }

    public IReadOnlyList<(string Label, string Href)> GetBreadcrumbs()
    {
        var crumbs = new List<(string Label, string Href)>();
        var segments = Page.Route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var path = "/";

        crumbs.Add(("Home", "/"));

        foreach (var segment in segments)
        {
            path += segment + "/";
            var matchedPage = AllPages.FirstOrDefault(p => p.Route == path);
            var label = matchedPage?.Title ?? segment;
            crumbs.Add((label, path));
        }

        return crumbs;
    }
}
