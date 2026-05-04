using RazorBlogGenerator.Models.Attributes;

namespace RazorBlogGenerator.Models;

[ContentSchema("index")]
public class IndexModel : ContentPage
{
    public string? Description { get; set; }

    public bool Hidden { get; set; } = false;
}
