using System.Collections.Generic;

namespace RazorBlogGenerator.Models.CV;

public class RoleData
{
    public string Name { get; set; } = null!;
    public string Period { get; set; } = string.Empty;
    public List<string> Bullets { get; set; } = [];
}