using RazorBlogGenerator.Models.Attributes;
using YamlDotNet.Serialization;

namespace RazorBlogGenerator.Models;

[ContentSchema]
public class PostModel : ContentPage
{
    public string PublishedOn { get; set; } = null!;
    public string Excerpt { get; set; } = null!;
    public List<string> Tags { get; set; } = [];
    public bool Hidden { get; set; }

    [YamlIgnore]
    public string RenderedHtml { get; set; } = string.Empty;
}
