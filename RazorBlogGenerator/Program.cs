using CommandLine;
using RazorBlogGenerator;
using RazorBlogGenerator.Commands;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

await Parser.Default.ParseArguments<BuildOptions, SchemaOptions, ValidateOptions, WatchOptions, ServeOptions>(args)
    .MapResult(
        async (BuildOptions opts) =>
        {
            var root = FindProjectRoot();
            await SiteGenerator.GenerateAsync(
                dataDir: FindDataDir(root),
                templatesDir: Path.Combine(root, "Templates"),
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
                FindDataDir(root),
                opts.Schemas ?? Path.Combine(root, "Schemas"));
            Environment.ExitCode = exitCode > 0 ? 1 : 0;
        },
        async (WatchOptions opts) =>
        {
            var root = FindProjectRoot();
            var distDir = opts.Output ?? Path.Combine(root, "dist");
            await DevServer.RunAsync(
                FindDataDir(root),
                Path.Combine(root, "Templates"),
                distDir,
                opts.Port);
        },
        async (ServeOptions opts) =>
        {
            var root = FindProjectRoot();
            var distDir = opts.Output ?? Path.Combine(root, "dist");
            var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var envPort)
                ? envPort
                : opts.Port;
            await StaticServer.RunAsync(distDir, port);
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

static string FindDataDir(string projectRoot)
{
    var candidates = new[]
    {
        Path.Combine(projectRoot, "Data"),
        Path.GetFullPath(Path.Combine(projectRoot, "..", "Data"))
    };

    return candidates.FirstOrDefault(Directory.Exists)
        ?? throw new DirectoryNotFoundException("Could not find the Data directory.");
}
