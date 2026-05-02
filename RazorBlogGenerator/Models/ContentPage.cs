using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RazorBlogGenerator.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum ContentType
{
    [EnumMember(Value = "yaml")]
    Yaml,

    [EnumMember(Value = "markdown")]
    Markdown
}

public class ContentPage
{
    public string Model { get; set; } = "";
    public string Template { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public ContentType ContentType { get; set; } = ContentType.Yaml;
}
