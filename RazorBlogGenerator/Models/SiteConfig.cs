using RazorBlogGenerator.Models.Attributes;

namespace RazorBlogGenerator.Models;

[ContentSchema]
public class SiteConfig
{
    public string SiteName { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
    public List<NavLink> Navbar { get; set; } = [];
}