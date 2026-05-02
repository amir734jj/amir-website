using CommandLine;

[Verb("build", isDefault: true, HelpText = "Generate the static site.")]
public class BuildOptions
{
    [Option('o', "output", HelpText = "Output directory for generated HTML.")]
    public string? Output { get; set; }
}