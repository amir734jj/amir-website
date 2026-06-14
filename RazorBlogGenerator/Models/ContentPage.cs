using YamlDotNet.Serialization;

namespace RazorBlogGenerator.Models;

public class ContentPage
{
    public string Model { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string> Vars { get; set; } = [];

    [YamlIgnore]
    public string Route { get; set; } = "/";
}
