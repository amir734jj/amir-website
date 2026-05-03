using System;
using System.Collections.Generic;
using System.Linq;

namespace RazorBlogGenerator.Models;

public class RenderContext
{
    public required ContentPage Page { get; init; }
    public required IReadOnlyList<ContentPage> AllPages { get; init; }
    public required SiteConfig Site { get; init; }
    public string BodyHtml { get; init; } = "";

    public IReadOnlyList<PostModel> GetAllDescendants()
    {
        var currentRoute = Page.Route.TrimEnd('/') + "/";
        return AllPages
            .OfType<PostModel>()
            .Where(p => !p.Hidden && p.Route != Page.Route && p.Route.StartsWith(currentRoute))
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
