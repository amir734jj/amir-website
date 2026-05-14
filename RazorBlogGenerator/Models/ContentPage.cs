using YamlDotNet.Serialization;

namespace RazorBlogGenerator.Models;

public class ContentPage
{
    public string Model { get; set; } = "";
    public string Template { get; set; } = "";
    public string Title { get; set; } = "";
    public Dictionary<string, string> Vars { get; set; } = [];

    [YamlIgnore]
    public string Route { get; set; } = "/";
}
