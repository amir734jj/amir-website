using CommandLine;

namespace RazorBlogGenerator.Commands;

[Verb("schema", HelpText = "Generate JSON schemas from C# models.")]
public class SchemaOptions
{
    [Option('o', "output", HelpText = "Output directory for schema files.")]
    public string? Output { get; set; }
}