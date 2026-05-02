using CommandLine;

namespace RazorBlogGenerator.Commands;

[Verb("watch", HelpText = "Watch for changes, rebuild, and serve locally.")]
public class WatchOptions
{
    [Option('o', "output", HelpText = "Output directory for generated HTML.")]
    public string? Output { get; set; }

    [Option('p', "port", Default = 8080, HelpText = "Port for the local web server.")]
    public int Port { get; set; }
}
