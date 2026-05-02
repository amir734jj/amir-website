namespace RazorBlogGenerator.Models;

public class RenderContext
{
    public required ContentPage Page { get; init; }
    public required IReadOnlyList<ContentPage> AllPages { get; init; }
    public required SiteConfig Site { get; init; }
    public string BodyHtml { get; init; } = "";
}
