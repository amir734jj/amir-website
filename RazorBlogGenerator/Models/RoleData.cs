namespace RazorBlogGenerator.Models;

public class RoleData
{
    public string Name { get; set; } = null!;
    public string Period { get; set; } = string.Empty;
    public List<string> Bullets { get; set; } = [];
}