using System.Globalization;
using NJsonSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorBlogGenerator.Models.Attributes;
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
        typeof(ContentSchemaAttribute).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(ContentSchemaAttribute), false).Length > 0)
            .ToDictionary(t => t.Name, ContentSchemaAttribute.GetSchemaName);

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

        foreach (var file in Directory.GetFiles(dataDir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Equals("site.yaml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("site.yml", StringComparison.OrdinalIgnoreCase))
            {
                errorCount += ValidateFile(file, schemaMap.GetValueOrDefault("site-config"));
                continue;
            }

            var yaml = await File.ReadAllTextAsync(file);

            var meta = YamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
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
                Log.Warning("{File}: no schema found for '{SchemaName}'", file, schemaName);
                errorCount++;
                continue;
            }

            errorCount += ValidateYamlAgainstSchema(file, yaml, schema);
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

    private static JToken YamlObjectToJToken(object? obj)
    {
        return obj switch
        {
            null => JValue.CreateNull(),
            Dictionary<object, object> dict => new JObject(
                dict.Select(kv => new JProperty(kv.Key.ToString()!, YamlObjectToJToken(kv.Value)))),
            List<object> list => new JArray(list.Select(YamlObjectToJToken)),
            string s when s is "true" or "True" or "TRUE" => new JValue(true),
            string s when s is "false" or "False" or "FALSE" => new JValue(false),
            string s when long.TryParse(s, out var l) => new JValue(l),
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => new JValue(d),
            _ => new JValue(obj)
        };
    }

    private static int ValidateYamlAgainstSchema(string file, string yaml, JsonSchema schema)
    {
        var yamlObject = YamlDeserializer.Deserialize<object>(yaml);
        var json = YamlObjectToJToken(yamlObject).ToString(Formatting.None);

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
