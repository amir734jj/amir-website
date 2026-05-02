using CommandLine;

namespace RazorBlogGenerator.Commands;

[Verb("validate", HelpText = "Validate YAML data files against JSON schemas.")]
public class ValidateOptions
{
    [Option('s', "schemas", HelpText = "Directory containing JSON schema files.")]
    public string? Schemas { get; set; }
}
