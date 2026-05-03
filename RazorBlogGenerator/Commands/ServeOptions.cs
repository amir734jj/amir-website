using CommandLine;

namespace RazorBlogGenerator.Commands;

[Verb("serve", HelpText = "Serve the generated site with case-insensitive routing.")]
public class ServeOptions
{
    [Option('o', "output", HelpText = "Directory containing generated HTML.")]
    public string? Output { get; set; }

    [Option('p', "port", Default = 8080, HelpText = "Port for the web server.")]
    public int Port { get; set; }
}
