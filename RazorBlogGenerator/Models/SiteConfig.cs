using RazorBlogGenerator.Models.Attributes;

namespace RazorBlogGenerator.Models;

[SchemaName]
public class SiteConfig
{
    public string SiteName { get; set; } = "";
    public string FooterText { get; set; } = "";
    public List<NavLink> Navbar { get; set; } = [];
}