using CommandLine;
using RazorBlogGenerator;
using RazorBlogGenerator.Commands;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

await Parser.Default.ParseArguments<BuildOptions, SchemaOptions, ValidateOptions, WatchOptions>(args)
    .MapResult(
        async (BuildOptions opts) =>
        {
            var root = FindProjectRoot();
            await SiteGenerator.GenerateAsync(
                dataDir: Path.Combine(root, "Data"),
                templatesDir: Path.Combine(root, "Templates"),
                staticDir: Path.Combine(root, "Static"),
                distDir: opts.Output ?? Path.Combine(root, "dist"));
        },
        async (SchemaOptions opts) =>
        {
            var root = FindProjectRoot();
            await SchemaGenerator.GenerateAsync(
                opts.Output ?? Path.Combine(root, "Schemas"));
        },
        async (ValidateOptions opts) =>
        {
            var root = FindProjectRoot();
            var exitCode = await YamlValidator.ValidateAsync(
                Path.Combine(root, "Data"),
                opts.Schemas ?? Path.Combine(root, "Schemas"));
            Environment.ExitCode = exitCode > 0 ? 1 : 0;
        },
        async (WatchOptions opts) =>
        {
            var root = FindProjectRoot();
            var distDir = opts.Output ?? Path.Combine(root, "dist");
            await DevServer.RunAsync(
                Path.Combine(root, "Data"),
                Path.Combine(root, "Templates"),
                Path.Combine(root, "Static"),
                distDir,
                opts.Port);
        },
        errors =>
        {
            var errs = errors.ToList();
            if (!errs.Any(e => e is HelpRequestedError or VersionRequestedError))
            {
                Log.Error("Unknown or invalid command. Run with --help for usage.");
                Environment.ExitCode = 1;
            }
            return Task.FromResult(1);
        });
return;

static string FindProjectRoot()
{
    var dir = AppContext.BaseDirectory;
    while (dir != null)
    {
        if (Directory.GetFiles(dir, "*.csproj").Length > 0)
        {
            return dir;
        }

        dir = Directory.GetParent(dir)?.FullName;
    }
    return Directory.GetCurrentDirectory();
}
