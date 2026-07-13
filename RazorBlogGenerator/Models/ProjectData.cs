namespace RazorBlogGenerator.Models;

public class ProjectData
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? GithubUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? BlogUrl { get; set; }
}
