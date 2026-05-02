using NJsonSchema;
using Newtonsoft.Json;
using RazorBlogGenerator.Models;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RazorBlogGenerator;

public static class YamlValidator
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly Dictionary<string, string> ModelToSchemaMap =
        typeof(SchemaNameAttribute).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(SchemaNameAttribute), false).Length > 0)
            .ToDictionary(t => t.Name, SchemaNameAttribute.GetSchemaName);

    public static async Task<int> ValidateAsync(string dataDir, string schemasDir)
    {
        var schemaMap = new Dictionary<string, JsonSchema>();
        foreach (var file in Directory.GetFiles(schemasDir, "*.schema.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file).Replace(".schema", "");
            var schema = await JsonSchema.FromFileAsync(file);
            schemaMap[name] = schema;
        }

        var errorCount = 0;

        foreach (var file in Directory.GetFiles(dataDir, "*.yaml", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file).Equals("site.yaml", StringComparison.OrdinalIgnoreCase))
            {
                errorCount += ValidateFile(file, schemaMap.GetValueOrDefault("site-config"));
                continue;
            }

            var raw = await File.ReadAllTextAsync(file);
            var parts = raw.Split("---", 2, StringSplitOptions.None);
            var metaYaml = parts[0].Trim();

            var meta = YamlDeserializer.Deserialize<Dictionary<string, object>>(metaYaml);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (meta is null || !meta.TryGetValue("model", out var modelObj))
            {
                Log.Warning("{File}: missing 'model' field, skipping", file);
                errorCount++;
                continue;
            }

            var modelName = modelObj.ToString()!;
            if (!ModelToSchemaMap.TryGetValue(modelName, out var schemaName))
            {
                Log.Warning("{File}: no schema mapping for model '{Model}'", file, modelName);
                errorCount++;
                continue;
            }

            if (!schemaMap.TryGetValue(schemaName, out var schema))
            {
                Log.Warning("{File}: no schema found for model '{Model}'", file, modelName);
                errorCount++;
                continue;
            }

            // For markdown content types, validate only the frontmatter
            // For yaml content types with a body, validate the body; otherwise validate frontmatter
            var contentType = meta.TryGetValue("content_type", out var ct) ? ct.ToString() : "yaml";
            string yamlToValidate;

            if (contentType == "markdown")
            {
                yamlToValidate = metaYaml;
            }
            else
            {
                yamlToValidate = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
                    ? parts[1].Trim()
                    : metaYaml;
            }

            errorCount += ValidateYamlAgainstSchema(file, yamlToValidate, schema);
        }

        if (errorCount == 0)
        {
            Log.Information("All YAML files are valid");
        }
        else
        {
            Log.Error("{Count} validation error(s) found", errorCount);
        }

        return errorCount;
    }

    private static int ValidateFile(string file, JsonSchema? schema)
    {
        if (schema == null)
        {
            Log.Warning("{File}: no schema found", file);
            return 1;
        }

        var yaml = File.ReadAllText(file);
        return ValidateYamlAgainstSchema(file, yaml, schema);
    }

    private static int ValidateYamlAgainstSchema(string file, string yaml, JsonSchema schema)
    {
        var yamlObject = YamlDeserializer.Deserialize<object>(yaml);
        var json = JsonConvert.SerializeObject(yamlObject);

        var validationErrors = schema.Validate(json);
        if (validationErrors.Count == 0)
        {
            Log.Information("{File}: valid", file);
            return 0;
        }

        foreach (var error in validationErrors)
        {
            Log.Error("{File}: {Kind} at {Path}", file, error.Kind, error.Path);
        }

        return validationErrors.Count;
    }
}
