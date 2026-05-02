using YamlDotNet.Serialization;

namespace RazorBlogGenerator.Models;

[SchemaName]
public class PostModel : ContentPage
{
    public string PublishedOn { get; set; } = null!;
    public string Excerpt { get; set; } = null!;

    [YamlIgnore]
    public string RenderedHtml { get; set; } = "";
}
